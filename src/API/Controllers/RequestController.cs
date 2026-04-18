using System.Security.Claims;
using Application.Interfaces.Trip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/requests")]
[Authorize]
public class RequestController : ControllerBase
{
    private readonly ITripService _tripService;

    public RequestController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> AcceptRequest([FromRoute] Guid id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var driverId))
        {
            return Unauthorized(new { message = "Missing or invalid user identifier in token." });
        }

        await _tripService.AcceptRequest(id, driverId);
        return Ok(new { message = "Ride request accepted successfully." });
    }
}
