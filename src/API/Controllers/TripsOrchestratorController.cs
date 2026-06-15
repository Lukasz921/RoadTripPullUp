using System.Security.Claims;
using API.DTOs;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripService.Application;
using Users.Application.DTOs;
using Users.Application.Interfaces;

namespace API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class TripsOrchestratorController : ControllerBase
{
    private readonly ITripsService _trips;
    private readonly IConversationService _conversations;
    private readonly IUserService _users;
    private readonly ILogger<TripsOrchestratorController> _logger;

    public TripsOrchestratorController(
        ITripsService trips,
        IConversationService conversations,
        IUserService users,
        ILogger<TripsOrchestratorController> logger)
    {
        _trips         = trips;
        _conversations = conversations;
        _users         = users;
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
                    Participants = [Guid.Parse(driverId)],
                    Type         = "group"
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

    // Passenger asks about a trip: create the direct chat with the driver AND a trip request
    // storing where they want to be picked up / dropped off. Reuses an existing open request.
    [HttpPost("trips/{tripId}/requests")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateTripRequest(
        [FromRoute] string tripId,
        [FromBody] CreateTripRequestDTO dto)
    {
        var passengerId = GetUserId();
        if (passengerId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var trip = await _trips.GetTripAsync(tripId); // 404 if the trip doesn't exist

        // A second click reuses the existing open request + its conversation (no orphan chat).
        var existing = await _trips.GetPendingTripRequestAsync(tripId, passengerId);
        if (existing is not null)
            return Ok(new { conversationId = existing.ConversationId, requestId = existing.Id, detourMeters = existing.DetourMeters });

        var conversationId = await _conversations.CreateConversationAsync(
            new CreateConversationDto
            {
                TripId       = Guid.Parse(tripId),
                Title        = "Trip request",
                Participants = [Guid.Parse(trip.DriverId)],
                Type         = "direct"
            },
            Guid.Parse(passengerId));

        var request = await _trips.CreateTripRequestAsync(tripId, passengerId, conversationId, dto.Pickup, dto.Dropoff);

        return Ok(new { conversationId = request.ConversationId, requestId = request.Id, detourMeters = request.DetourMeters });
    }

    // Driver accepts a request: recompute the trip route through the new stops, add the passenger,
    // and add them to the trip's group chat (best-effort, mirroring AddPassenger).
    [HttpPost("trips/{tripId}/requests/{requestId}/accept")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> AcceptTripRequest(
        [FromRoute] string tripId,
        [FromRoute] string requestId)
    {
        var driverId = GetUserId();
        if (driverId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var requesterId = await _trips.AcceptTripRequestAsync(tripId, driverId, requestId);

        try
        {
            await _conversations.AddMemberToTripGroupAsync(Guid.Parse(tripId), Guid.Parse(requesterId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add requester {RequesterId} to group chat for trip {TripId}", requesterId, tripId);
        }

        return NoContent();
    }

    // Chat panel: fetch the trip request behind a direct conversation (pickup/dropoff, detour, preview route).
    [HttpGet("trips/requests/by-conversation/{conversationId}")]
    [ProducesResponseType(typeof(TripRequestDTO), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTripRequestByConversation([FromRoute] string conversationId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = new { code = "UNAUTHORIZED", message = "Missing or invalid token." } });

        var request = await _trips.GetTripRequestByConversationAsync(conversationId);
        if (request is null) return NotFound();
        return Ok(request);
    }

    [HttpPost("trips/{tripId}/rate-user")]
    public async Task<IActionResult> RateUser([FromRoute] string tripId, [FromBody] RateUserDTO dto)
    {
        var currentUserIdStr = GetUserId();
        if (currentUserIdStr == null) return Unauthorized();
        var currentUserId = Guid.Parse(currentUserIdStr);

        var trip = await _trips.GetTripAsync(tripId);
        if (trip == null) return NotFound("Trip not found.");

        if (trip.DepartureTime.ToUniversalTime() > DateTime.UtcNow)
            return BadRequest(new { error = new { code = "TRIP_NOT_COMPLETED", message = "You can only rate users after the trip has taken place." } });

        // Check if current user participated in the trip
        bool isCurrentUserDriver = trip.DriverId == currentUserIdStr;
        bool isCurrentUserPassenger = trip.PassengerIds.Contains(currentUserIdStr);

        if (!isCurrentUserDriver && !isCurrentUserPassenger)
        {
            return Forbid();
        }

        // Check if target user participated in the trip
        string targetUserIdStr = dto.UserId.ToString();
        bool isTargetDriver = trip.DriverId == targetUserIdStr;
        bool isTargetPassenger = trip.PassengerIds.Contains(targetUserIdStr);

        if (!isTargetDriver && !isTargetPassenger)
        {
            return BadRequest("Target user didn't participate in this trip.");
        }

        if (currentUserId == dto.UserId)
        {
            return BadRequest("You cannot rate yourself.");
        }

        // Only passengers can rate the driver for now as per requirements
        if (targetUserIdStr != trip.DriverId)
        {
            return BadRequest("Currently only driver ratings are supported via trip ratings.");
        }

        await _trips.RateTripAsync(tripId, currentUserIdStr, new RateTripDTO { Rating = dto.Value });
        await _users.UpdateUserRating(dto.UserId, dto.Value);

        return Ok();
    }

    [HttpPost("trips/{tripId}/complaint")]
    public async Task<IActionResult> FileComplaint([FromRoute] string tripId, [FromBody] FileComplaintDTO dto)
    {
        var currentUserIdStr = GetUserId();
        if (currentUserIdStr == null) return Unauthorized();
        var currentUserId = Guid.Parse(currentUserIdStr);

        var trip = await _trips.GetTripAsync(tripId);
        if (trip == null) return NotFound("Trip not found.");

        // Check if current user participated in the trip
        bool isCurrentUserDriver = trip.DriverId == currentUserIdStr;
        bool isCurrentUserPassenger = trip.PassengerIds.Contains(currentUserIdStr);

        if (!isCurrentUserDriver && !isCurrentUserPassenger)
        {
            return Forbid();
        }

        // Check if target user participated in the trip
        string targetUserIdStr = dto.ComplainedUserId.ToString();
        bool isTargetDriver = trip.DriverId == targetUserIdStr;
        bool isTargetPassenger = trip.PassengerIds.Contains(targetUserIdStr);

        if (!isTargetDriver && !isTargetPassenger)
        {
            return BadRequest(new { error = new { code = "TARGET_NOT_PARTICIPANT", message = "The complained user did not participate in this trip." } });
        }
        
        if (currentUserId == dto.ComplainedUserId)
        {
            return BadRequest(new { error = new { code = "CANNOT_COMPLAIN_ABOUT_SELF", message = "You cannot file a complaint against yourself." } });
        }

        await _users.FileComplaint(currentUserId, Guid.Parse(tripId), dto);

        return NoContent();
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue("sub");
}
