using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces.Trip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
// property authorize dleguje najpierw do authorize zdefinowanego w program.cs 
[Authorize]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTripDTO dto)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue("sub");

            if (!Guid.TryParse(userIdClaim, out var driverId))
            {
                return Unauthorized(new { message = "Missing or invalid user identifier in token." });
            }

            var createdTrip = await _tripService.CreateTrip(dto, driverId);
            return Created($"/api/trip/{createdTrip.TripId}", createdTrip);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] SearchTripsCriteria criteria)
    {
        try
        {
            var results = await _tripService.SearchTrips(criteria);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
