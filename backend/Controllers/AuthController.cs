using System.Security.Claims;
using backend.Models.Request;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IUserService users) : ControllerBase
{
    private int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await users.RegisterAsync(request, ClientIp, cancellationToken);
        return !result.Succeeded
            ? Conflict(new { message = result.Message })
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await users.LoginAsync(request, ClientIp, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Unauthorized(new { message = result.Message });
    }

    [HttpPost("two-factor/verify"), AllowAnonymous]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await users.VerifyTwoFactorLoginAsync(request, ClientIp, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : Unauthorized(new { message = result.Message });
    }

    [Authorize, HttpPost("two-factor/setup")]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken cancellationToken)
    {
        var result = await users.SetupTwoFactorAsync(UserId, ClientIp, cancellationToken);
        return result.Succeeded
            ? Ok(result.Value)
            : NotFound(new { message = result.Message });
    }

    [Authorize, HttpPost("two-factor/enable")]
    public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await users.EnableTwoFactorAsync(UserId, request, ClientIp, cancellationToken);
        return !result.Succeeded
            ? BadRequest(new { message = result.Message })
            : Ok(result.Value);
    }

    [Authorize, HttpDelete("two-factor")]
    public async Task<IActionResult> DisableTwoFactor(EnableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await users.DisableTwoFactorAsync(UserId, request, ClientIp, cancellationToken);
        return !result.Succeeded
            ? BadRequest(new { message = result.Message })
            : Ok(result.Value);
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(UserId, cancellationToken);
        return user is null ? NotFound(new { message = "User not found." }) : Ok(user);
    }
}
