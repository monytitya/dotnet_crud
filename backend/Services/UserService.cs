using backend.Data;
using backend.Models.Data;
using backend.Models.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public sealed class UserService(
    AppDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    JwtService jwtService,
    TwoFactorService twoFactorService,
    IConfiguration configuration) : IUserService
{
    public async Task<UserServiceResult<UserResponse>> RegisterAsync(
        RegisterRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var duplicate = await db.Users.AnyAsync(user =>
            user.Username.ToLower() == username.ToLower() || user.Email == email, cancellationToken);
        if (duplicate)
        {
            await AddAuditAsync(null, username, "registration", false, ipAddress, "Username or email already exists.", cancellationToken);
            return new(null, UserServiceError.DuplicateAccount, "Username or email already exists.");
        }

        var user = new AppUser { Username = username, Email = email, PasswordHash = string.Empty };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await AddAuditAsync(user.Id, username, "registration", true, ipAddress, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(new UserResponse(user.Id, user.Username, user.Email), UserServiceError.None, null);
    }

    public async Task<UserServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await db.Users.FirstOrDefaultAsync(
            item => item.Username.ToLower() == username.ToLower(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            await AddAuditAsync(user?.Id, username, "login", false, ipAddress, "Invalid credentials.", cancellationToken);
            return new(null, UserServiceError.InvalidCredentials, "Invalid username or password.");
        }
        if (user.LockoutEndUtc > DateTimeOffset.UtcNow)
        {
            await AddAuditAsync(user.Id, user.Username, "lockout", false, ipAddress, "Login attempted during lockout.", cancellationToken);
            return new(null, UserServiceError.LockedOut, "The account is temporarily locked.");
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            var locked = user.FailedLoginCount >= 5;
            if (locked)
            {
                user.LockoutEndUtc = DateTimeOffset.UtcNow.AddMinutes(15);
                user.FailedLoginCount = 0;
            }
            await AddAuditAsync(user.Id, user.Username, locked ? "lockout" : "login", false, ipAddress,
                locked ? "Five failed login attempts." : "Invalid password.", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return new(null, locked ? UserServiceError.LockedOut : UserServiceError.InvalidCredentials,
                locked ? "The account is temporarily locked." : "Invalid username or password.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        await AddAuditAsync(user.Id, user.Username, "login", true, ipAddress, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (user.TwoFactorEnabled)
        {
            var challenge = jwtService.CreateTwoFactorChallenge(user);
            return new(new LoginResponse(true, null, challenge.Token, challenge.ExpiresAt), UserServiceError.None, null);
        }
        var access = jwtService.CreateAccessToken(user);
        return new(new LoginResponse(false, access.Token, null, access.ExpiresAt), UserServiceError.None, null);
    }

    public async Task<UserServiceResult<TwoFactorSetupResponse>> SetupTwoFactorAsync(
        int userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null) return new(null, UserServiceError.NotFound, "User not found.");
        var secret = twoFactorService.GenerateSecretKey();
        var issuer = configuration["Jwt:Issuer"] ?? "dotnet_angular";
        var uri = twoFactorService.CreateOtpAuthUri(user.Username, issuer, secret);
        var qr = twoFactorService.CreateQrCodeDataUrl(uri);
        user.TwoFactorSecret = secret;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await AddAuditAsync(user.Id, user.Username, "two-factor-setup", true, ipAddress, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(new TwoFactorSetupResponse(secret, uri, qr), UserServiceError.None, null);
    }

    public async Task<UserServiceResult<MessageResponse>> EnableTwoFactorAsync(
        int userId, EnableTwoFactorRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null) return new(null, UserServiceError.NotFound, "User not found.");
        if (!twoFactorService.VerifyCode(user.TwoFactorSecret, request.TwoFactorCode))
        {
            await AddAuditAsync(user.Id, user.Username, "two-factor-enable", false, ipAddress, "Invalid verification code.", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return new(null, UserServiceError.InvalidCode, "Invalid code or setup not started.");
        }
        user.TwoFactorEnabled = true;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await AddAuditAsync(user.Id, user.Username, "two-factor-enable", true, ipAddress, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(new MessageResponse("Two-factor authentication enabled."), UserServiceError.None, null);
    }

    public async Task<UserServiceResult<LoginResponse>> VerifyTwoFactorLoginAsync(
        VerifyTwoFactorRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var principal = jwtService.ValidateChallenge(request.ChallengeToken);
        if (principal is null || !int.TryParse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
            return new(null, UserServiceError.InvalidChallenge, "Invalid two-factor challenge.");
        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null || !user.IsActive || !user.TwoFactorEnabled)
            return new(null, UserServiceError.InvalidChallenge, "Invalid two-factor challenge.");
        if (!twoFactorService.VerifyCode(user.TwoFactorSecret, request.TwoFactorCode))
        {
            await AddAuditAsync(user.Id, user.Username, "two-factor-login", false, ipAddress, "Invalid verification code.", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return new(null, UserServiceError.InvalidCode, "Invalid two-factor code.");
        }
        var access = jwtService.CreateAccessToken(user);
        await AddAuditAsync(user.Id, user.Username, "two-factor-login", true, ipAddress, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(new LoginResponse(false, access.Token, null, access.ExpiresAt), UserServiceError.None, null);
    }

    public async Task<UserServiceResult<MessageResponse>> DisableTwoFactorAsync(
        int userId, EnableTwoFactorRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null) return new(null, UserServiceError.NotFound, "User not found.");
        if (!user.TwoFactorEnabled || !twoFactorService.VerifyCode(user.TwoFactorSecret, request.TwoFactorCode))
        {
            await AddAuditAsync(user.Id, user.Username, "two-factor-disable", false, ipAddress, "Invalid verification code.", cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return new(null, UserServiceError.InvalidCode, "Invalid two-factor code.");
        }
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await AddAuditAsync(user.Id, user.Username, "two-factor-disable", true, ipAddress, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new(new MessageResponse("Two-factor authentication disabled."), UserServiceError.None, null);
    }

    public async Task<UserResponse?> GetByIdAsync(int userId, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new UserResponse(user.Id, user.Username, user.Email))
            .FirstOrDefaultAsync(cancellationToken);

    private async Task AddAuditAsync(
        int? userId, string? username, string eventType, bool success, string? ipAddress,
        string? details, CancellationToken cancellationToken)
    {
        db.AuthAuditLogs.Add(new AuthAuditLog
        {
            UserId = userId, Username = username, EventType = eventType, Success = success,
            IpAddress = ipAddress, Details = details
        });
        await Task.CompletedTask;
    }
}
