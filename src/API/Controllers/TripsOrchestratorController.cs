using System.Security.Claims;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripService.Application;

namespace API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class TripsOrchestratorController : ControllerBase
{
    private readonly ITripsService _trips;
    private readonly IConversationService _conversations;
    private readonly ILogger<TripsOrchestratorController> _logger;

    public TripsOrchestratorController(
        ITripsService trips,
        IConversationService conversations,
        ILogger<TripsOrchestratorController> logger)
    {
        _trips         = trips;
        _conversations = conversations;
        _logger        = logger;
    }

    [HttpPost("trips")]
    [ProducesResponseType(typeof(TripDTO), 201)]
    public async Task<IActionResult> CreateTrip([FromBody] CreateTripDTO dto)
    {
        var driverId = GetUserId();
        if (driverId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var trip = await _trips.CreateTripAsync(dto, driverId);

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

        await _trips.AddPassengerAsync(tripId, driverId, dto.PassengerId);

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

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue("sub");
}
