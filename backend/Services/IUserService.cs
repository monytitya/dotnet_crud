using backend.Models.Request;

namespace backend.Services;

public interface IUserService
{
    Task<UserServiceResult<UserResponse>> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<UserServiceResult<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<UserServiceResult<TwoFactorSetupResponse>> SetupTwoFactorAsync(int userId, string? ipAddress, CancellationToken cancellationToken);
    Task<UserServiceResult<MessageResponse>> EnableTwoFactorAsync(int userId, EnableTwoFactorRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<UserServiceResult<LoginResponse>> VerifyTwoFactorLoginAsync(VerifyTwoFactorRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<UserServiceResult<MessageResponse>> DisableTwoFactorAsync(int userId, EnableTwoFactorRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<UserResponse?> GetByIdAsync(int userId, CancellationToken cancellationToken);
}

public enum UserServiceError { None, DuplicateAccount, InvalidCredentials, LockedOut, InvalidChallenge, InvalidCode, NotFound }

public sealed record UserServiceResult<T>(T? Value, UserServiceError Error, string? Message)
{
    public bool Succeeded => Error == UserServiceError.None;
}
