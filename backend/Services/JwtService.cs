using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Models.Data;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public sealed class JwtService(IConfiguration configuration)
{
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AppUser user) => CreateToken(user, challenge: false);

    public (string Token, DateTimeOffset ExpiresAt) CreateTwoFactorChallenge(AppUser user) => CreateToken(user, challenge: true);

    public ClaimsPrincipal? ValidateChallenge(string token)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, GetTokenValidationParameters(), out _);
            return principal.FindFirst("token_type")?.Value == "2fa-challenge" ? principal : null;
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    public TokenValidationParameters GetTokenValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    private (string Token, DateTimeOffset ExpiresAt) CreateToken(AppUser user, bool challenge)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(challenge ? 5 : 60);
        var authenticationMethod = challenge ? "mfa" : user.TwoFactorEnabled ? "pwd+mfa" : "pwd";
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("token_type", challenge ? "2fa-challenge" : "access"),
            new("amr", authenticationMethod),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
