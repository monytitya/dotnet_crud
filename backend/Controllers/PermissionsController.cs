using System.Security.Claims;
using backend.Models.Request;
using backend.Services.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/permissions")]
public sealed class PermissionsController(IPermissionService service) : ControllerBase
{
    private int ActorUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var permission = await service.GetByIdAsync(id, cancellationToken);
        return permission is null ? NotFound(new { message = "Permission not found." }) : Ok(permission);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, ActorUserId, cancellationToken);
        if (!result.Succeeded)
            return result.Error == PermissionServiceError.InvalidPage
                ? BadRequest(new { message = result.Message })
                : Conflict(new { message = result.Message });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePermissionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, ActorUserId, cancellationToken);
        return !result.Succeeded
            ? NotFound(new { message = result.Message })
            : Ok(result.Value);
    }
}
