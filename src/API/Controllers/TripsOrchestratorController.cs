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

        await _users.AddRating(new Users.Application.DTOs.AddRatingDTO
        {
            UserId = dto.UserId,
            RaterId = currentUserId,
            Value = dto.Value,
            Comment = dto.Comment
        });

        return Ok();
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue("sub");
}
