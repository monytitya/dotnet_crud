using System.Security.Claims;
using backend.Models.Request;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/roles")]
public sealed class RolesController(IRoleService service) : ControllerBase
{
    private int ActorUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> GetPage([FromQuery] RoleQueryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.GetPageAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var role = await service.GetByIdAsync(id, cancellationToken);
        return role is null ? NotFound(new { message = "Role not found." }) : Ok(role);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, ActorUserId, cancellationToken);
        if (!result.Succeeded) return Conflict(new { message = result.Message });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, ActorUserId, cancellationToken);
        return result.Error == RoleServiceError.NotFound
            ? NotFound(new { message = result.Message })
            : Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound(new { message = "Role not found." });

    [HttpPut("{id:int}/permissions")]
    public async Task<IActionResult> SetPermissions(int id, [FromBody] int[] ids, CancellationToken cancellationToken) =>
        await service.SetPermissionsAsync(id, ids, ActorUserId, cancellationToken)
            ? NoContent()
            : NotFound(new { message = "Role or permission not found." });
}
