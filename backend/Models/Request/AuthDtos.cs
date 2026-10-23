using System.ComponentModel.DataAnnotations;

namespace backend.Models.Request;

public sealed class RegisterRequest
{
    [Required, MinLength(3), MaxLength(100)] public string Username { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(254)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8), MaxLength(128)] public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public sealed class EnableTwoFactorRequest
{
    [Required, RegularExpression("^[0-9]{6}$")] public string TwoFactorCode { get; set; } = string.Empty;
}

public sealed class VerifyTwoFactorRequest
{
    [Required] public string ChallengeToken { get; set; } = string.Empty;
    [Required, RegularExpression("^[0-9]{6}$")] public string TwoFactorCode { get; set; } = string.Empty;
}

public sealed record LoginResponse(bool RequiresTwoFactor, string? AccessToken, string? ChallengeToken, DateTimeOffset? ExpiresAtUtc);
public sealed record UserResponse(int Id, string Username, string Email);
public sealed record MessageResponse(string Message);
public sealed record TwoFactorSetupResponse(string Secret, string OtpAuthUri, string QrCodeDataUrl);
