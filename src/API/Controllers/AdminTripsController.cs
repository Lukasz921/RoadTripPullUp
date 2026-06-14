using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripService.Application;

namespace API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN", Policy = "NotBanned")]
public class AdminTripsController : ControllerBase
{
    private readonly ITripsService _trips;

    public AdminTripsController(ITripsService trips)
    {
        _trips = trips;
    }

    [HttpGet("trips")]
    [ProducesResponseType(typeof(PagedTripsDTO), 200)]
    public async Task<IActionResult> GetAllTrips(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var result = await _trips.GetAllTripsAsync(dateFrom, dateTo, page, pageSize);
        return Ok(result);
    }

    [HttpDelete("trips/{tripId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteTrip([FromRoute] string tripId)
    {
        await _trips.AdminDeleteTripAsync(tripId);
        return NoContent();
    }
}
