using System.Security.Claims;
using backend.Models.Request;
using backend.Services;
using backend.Services.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;
[ApiController,Authorize,Route("api/navigation")]
public sealed class NavigationController(INavigationService service):ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct){if(!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id))return Unauthorized(new{message="Invalid user token."});return Ok(await service.GetUserNavigationAsync(id,ct));}
}
