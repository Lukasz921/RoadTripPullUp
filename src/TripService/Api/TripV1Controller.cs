using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripService.Application;

namespace TripService.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public class TripV1Controller : ControllerBase
{
    private readonly ITripsV1Service _service;
    private readonly ITripsSearchService _search;

    public TripV1Controller(ITripsV1Service service, ITripsSearchService search)
    {
        _service = service;
        _search  = search;
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
    [ProducesResponseType(typeof(PagedTripsDTO), 200)]
    public async Task<IActionResult> GetMyTrips(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var driverId = GetUserId();
        if (driverId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var result = await _service.GetMyTripsAsync(driverId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("trips/joined")]
    [ProducesResponseType(typeof(PagedTripsDTO), 200)]
    public async Task<IActionResult> GetJoinedTrips(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var result = await _service.GetMyPassengerTripsAsync(userId, page, pageSize);
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

    [HttpPost("trips/{tripId}/join")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> JoinTrip([FromRoute] string tripId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        await _service.JoinTripAsync(tripId, userId);
        return NoContent();
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

    [HttpGet("trips/search")]
    [ProducesResponseType(typeof(SyncSearchResultDTO), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(502)]
    public async Task<IActionResult> SearchTrips([FromQuery] SearchTripsQueryDTO query, CancellationToken ct)
    {
        var result = await _search.SearchAsync(query, ct);
        return Ok(result);
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
