using System.Security.Claims;
using System.Globalization;
using Application.DTOs;
using Application.Interfaces.Trip;
using Application.Exceptions;
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

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<TripSummaryDTO>), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Search([FromQuery(Name = "from")] string? from, [FromQuery(Name = "to")] string? to, [FromQuery(Name = "date")] string? date)
    {
        DateTime? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(date))
        {
            // try yyyy-MM-dd first (date input)
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d))
            {
                parsedDate = DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
            }
            else if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out d))
            {
                parsedDate = DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
            }
            else
            {
                throw new ValidationException("Invalid date format. Use YYYY-MM-DD or a valid date.");
            }
        }

        var criteria = new SearchTripsCriteria
        {
            From = string.IsNullOrWhiteSpace(from) ? null : from,
            To = string.IsNullOrWhiteSpace(to) ? null : to,
            Date = parsedDate
        };

        var results = await _tripService.SearchTrips(criteria);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TripDetailsDTO), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var dto = await _tripService.GetById(id);
        return Ok(dto);
    }
}
