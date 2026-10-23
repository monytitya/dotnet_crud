using System.Security.Claims;
using backend.Models.Request;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController, Authorize, Route("api/units")]
public sealed class UnitsController(IUnitService service) : ControllerBase
{
    private int ActorUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> GetPage([FromQuery] UnitQueryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.GetPageAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var unit = await service.GetByIdAsync(id, cancellationToken);
        return unit is null ? NotFound(new { message = "Unit not found." }) : Ok(unit);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, ActorUserId, cancellationToken);
        if (!result.Succeeded) return ServiceError(result.Error, result.Message);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUnitRequest request, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, ActorUserId, cancellationToken);
        return !result.Succeeded ? ServiceError(result.Error, result.Message) : Ok(result.Value);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return !result.Succeeded ? NotFound(new { message = result.Message }) : NoContent();
    }

    private IActionResult ServiceError(UnitServiceError error, string? message) =>
        error == UnitServiceError.NotFound
            ? NotFound(new { message })
            : BadRequest(new { message });
}
