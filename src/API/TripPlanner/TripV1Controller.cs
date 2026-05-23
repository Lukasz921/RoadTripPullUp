using System.Security.Claims;
using Application.TripPlanner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.TripPlanner;

[ApiController]
[Route("api/v1")]
[Authorize]
public class TripV1Controller : ControllerBase
{
    private readonly ITripsV1Service _service;

    public TripV1Controller(ITripsV1Service service)
    {
        _service = service;
    }

    [HttpPost("trips")]
    [ProducesResponseType(typeof(TripV1DTO), 201)]
    public async Task<IActionResult> CreateTrip([FromBody] CreateTripV1DTO dto)
    {
        var driverId = GetUserId();
        if (driverId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var trip = await _service.CreateTripAsync(dto, driverId);
        return Created($"/api/v1/trips/{trip.Id}", trip);
    }

    [HttpGet("trips/me")]
    [ProducesResponseType(typeof(MyTripsV1ResultDTO), 200)]
    public async Task<IActionResult> GetMyTrips(
        [FromQuery] string status = "ACTIVE",
        [FromQuery] int limit = 50)
    {
        var driverId = GetUserId();
        if (driverId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var result = await _service.GetMyTripsAsync(driverId, status, limit);
        return Ok(result);
    }

    [HttpGet("trips/{tripId}")]
    [ProducesResponseType(typeof(TripV1DTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTrip([FromRoute] string tripId)
    {
        var trip = await _service.GetTripAsync(tripId);
        return Ok(trip);
    }

    [HttpDelete("trips/{tripId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteTrip([FromRoute] string tripId)
    {
        var driverId = GetUserId();
        if (driverId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        await _service.DeleteTripAsync(tripId, driverId);
        return NoContent();
    }

    [HttpPost("trips/search")]
    [ProducesResponseType(typeof(SearchJobCreatedDTO), 202)]
    public async Task<IActionResult> SubmitSearch([FromBody] SearchTripsV1RequestDTO dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var job = await _service.SubmitSearchAsync(dto, userId);
        return Accepted(job.StatusUrl, job);
    }

    [HttpGet("trips/search/{jobId}")]
    [ProducesResponseType(typeof(SearchJobProgressDTO), 202)]
    [ProducesResponseType(typeof(SearchJobResultDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PollSearch([FromRoute] string jobId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var poll = await _service.PollSearchJobAsync(jobId, userId);

        if (poll.IsProcessing)
            return StatusCode(202, poll.Progress);

        return Ok(poll.Result);
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue("sub");
}
