using System.Security.Claims;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using MessageService.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TripService.Application;

namespace TripService.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public class TripV1Controller : ControllerBase
{
    private readonly ITripsV1Service _service;
    private readonly ITripsSearchService _search;
    private readonly IConversationService _conversations;
    private readonly ILogger<TripV1Controller> _logger;

    public TripV1Controller(
        ITripsV1Service service,
        ITripsSearchService search,
        IConversationService conversations,
        ILogger<TripV1Controller> logger)
    {
        _service       = service;
        _search        = search;
        _conversations = conversations;
        _logger        = logger;
    }

    [HttpPost("trips")]
    [ProducesResponseType(typeof(TripV1DTO), 201)]
    public async Task<IActionResult> CreateTrip([FromBody] CreateTripV1DTO dto)
    {
        var driverId = GetUserId();
        if (driverId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var trip = await _service.CreateTripAsync(dto, driverId);

        try
        {
            var conversationId = await _conversations.CreateConversationAsync(
                new CreateConversationDto
                {
                    TripId       = Guid.Parse(trip.Id),
                    Title        = "Group Chat",
                    Date         = trip.DepartureTime,
                    Participants = [Guid.Parse(driverId)]
                },
                Guid.Parse(driverId));

            trip.ConversationId = conversationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create group chat for trip {TripId} — trip still created", trip.Id);
        }

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

    [HttpPost("trips/{tripId}/passengers")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> AddPassenger(
        [FromRoute] string tripId,
        [FromBody] AddPassengerDTO dto)
    {
        var driverId = GetUserId();
        if (driverId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        await _service.AddPassengerAsync(tripId, driverId, dto.PassengerId);

        try
        {
            await _conversations.AddMemberToTripGroupAsync(Guid.Parse(tripId), Guid.Parse(dto.PassengerId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add passenger {PassengerId} to group chat for trip {TripId}", dto.PassengerId, tripId);
        }

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
