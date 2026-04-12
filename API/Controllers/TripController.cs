using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces.Trip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/trips")]
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
            return Created($"/api/trips/{createdTrip.TripId}", createdTrip);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TripSummaryDTO>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Search([FromQuery(Name = "from")] string? from, [FromQuery(Name = "to")] string? to, [FromQuery(Name = "date")] DateTime? date)
    {
        try
        {
            var criteria = new SearchTripsCriteria
            {
                From = string.IsNullOrWhiteSpace(from) ? null : from,
                To = string.IsNullOrWhiteSpace(to) ? null : to,
                Date = date
            };

            var results = await _tripService.SearchTrips(criteria);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
