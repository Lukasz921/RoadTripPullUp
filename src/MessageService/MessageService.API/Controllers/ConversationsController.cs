using System.Security.Claims;
using MessageService.Application.DTOs;
using MessageService.Application.Helpers;
using MessageService.Application.Services;
using MessageService.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/v1/message/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversations;
    private readonly IClockService _clockService;

    public ConversationsController(IConversationService conversations, IClockService clockService)
    {
        _conversations = conversations;
        _clockService = clockService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationDto dto)
    {
        var userId = GetUserId();
        var id = await _conversations.CreateConversationAsync(dto, userId);
        return CreatedAtAction(nameof(Get), new { conversationId = id }, new { conversationId = id });
    }
    
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? fromConversation = null,
        [FromQuery] int? toConversation = null)
    {
        var userId = GetUserId();

        int skip;
        int take;

        if (fromConversation != null && toConversation != null)
        {
            if (fromConversation < 0 || toConversation < 0) return BadRequest(new { error = "fromConversation and toConversation must be non-negative" });
            if (toConversation < fromConversation) return BadRequest(new { error = "toConversation must be >= fromConversation" });

            skip = fromConversation.Value;
            // inclusive range: from..to => count = to - from + 1
            try
            {
                checked
                {
                    take = toConversation.Value - fromConversation.Value + 1;
                }
            }
            catch (OverflowException)
            {
                return BadRequest(new { error = "range too large" });
            }
        }
        else if (fromConversation != null)
        {
            if (fromConversation < 0) return BadRequest(new { error = "fromConversation must be non-negative" });
            skip = fromConversation.Value;
            take = 20; // default window size
        }
        else if (toConversation != null)
        {
            if (toConversation < 0) return BadRequest(new { error = "toConversation must be non-negative" });
            skip = 0;
            take = toConversation.Value + 1; // take first (to+1) items
        }
        else
        {
            // no range provided: default to first page/window
            skip = 0;
            take = 20;
        }

        // updatedAfter is currently not used by the repository's GetForUserAsync signature; keep it for future use
        var convs = await _conversations.GetForUserAsync(userId, skip, take);
        return Ok(convs);
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<IActionResult> Get(Guid conversationId)
    {
        var conv = await _conversations.GetByIdAsync(conversationId);
        if (conv == null) return NotFound();

        // authorization: ensure current user is member
        var userId = GetUserId();
        if (conv.Members.All(m => m.UserId != userId)) return Forbid();
        
        var lastMsg = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();

        var dto = new ConversationDto
        {
            ConversationId = conv.Id,
            Type = conv.Type,
            Name = conv.Title,
            Date = conv.Date,
            TripId = conv.TripId,
            Participants = conv.Members.Select(m => m.UserId).ToList(),
            LastMessageId = lastMsg?.Id ?? Guid.Empty,
            LastMessagePreview = lastMsg?.GetMessagePreview() ?? string.Empty,
            LastMessageCreatedAt = lastMsg?.CreatedAt ?? DateTime.UnixEpoch
        };

        return Ok(dto);
    }

    [HttpGet("byTripId/group/{tripId:guid}")]
    public async Task<IActionResult> GetGroupForTrip(Guid tripId)
    {
        var conv = await _conversations.GetGroupForTripAsync(tripId);
        var userId = GetUserId();
        if (conv == null) return NotFound();
        if (conv.Members.All(m => m.UserId != userId)) return Forbid();
        var dto = new ConversationDto
        {
            ConversationId = conv.Id,
            Type = conv.Type,
            Name = conv.Title,
            Date = conv.Date,
            TripId = conv.TripId,
            Participants = conv.Members.Select(m => m.UserId).ToList(),
            LastMessageId = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Id ?? Guid.Empty,
            LastMessagePreview = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.GetMessagePreview() ?? string.Empty,
            LastMessageCreatedAt = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.CreatedAt ??
                                   DateTime.UnixEpoch
        };
        return Ok(dto);
    }
    
    [HttpGet("/byTripId/direct/{tripId:guid}")]
    public async Task<IActionResult> GetDirectForTrip(Guid tripId)
    {
        var userId = GetUserId();
        var convs = await _conversations.GetDirectForTripAsync(tripId, userId);
        var dtos = convs.Select(conv => new ConversationDto
        {
            ConversationId = conv.Id,
            Type = conv.Type,
            Name = conv.Title,
            Date = conv.Date,
            TripId = conv.TripId,
            Participants = conv.Members.Select(m => m.UserId).ToList(),
            LastMessageId = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Id ?? Guid.Empty,
            LastMessagePreview = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.GetMessagePreview() ?? string.Empty,
            LastMessageCreatedAt = conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.CreatedAt ??
                                   DateTime.UnixEpoch
        });
        return Ok(dtos);
    }

    [HttpPost("{conversationId:guid}/join/{userId:guid}")]
    public async Task<IActionResult> Join(Guid conversationId, Guid userId)
    {
        var conv = await _conversations.GetByIdAsync(conversationId);
        if (conv == null) return NotFound();
        
        var driverId = GetUserId();
        if (conv.Members.All(m => m.UserId != driverId)) return Forbid();
        if (conv.Members.Any(m => m.UserId == userId)) return BadRequest(new { error = "already a member" });
        
        conv.Members.Add(new ConversationMember
        {
            UserId = userId,
            JoinedAt = _clockService.Now,
            Role = 0
        });
        // TODO: no validation that current user (driverId) is actually a driver or another member of conversation
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(sub) ? Guid.Empty : Guid.Parse(sub);
    }
}
