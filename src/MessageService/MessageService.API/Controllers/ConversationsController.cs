using System.Security.Claims;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using MessageService.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/v1/message/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversations;

    public ConversationsController(IConversationService conversations)
    {
        _conversations = conversations;
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
            Participants = conv.Members.Select(m => m.UserId).ToList(),
            LastMessageId = lastMsg?.Id ?? Guid.Empty,
            LastMessagePreview = GetMessagePreview(lastMsg),
            LastMessageCreatedAt = lastMsg?.CreatedAt ?? DateTime.UnixEpoch
        };

        return Ok(dto);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(sub) ? Guid.Empty : Guid.Parse(sub);
    }
    
    private static string GetMessagePreview(Message? msg) // TODO: move to a helper/extension method
    {
        if (msg == null) return string.Empty;

        return msg.Type switch
        {
            MessageType.Text => msg.Payload?["text"]?.ToString() ?? string.Empty,
            MessageType.Location => "[Location]",
            MessageType.PriceOffer => "[Price Offer]",
            MessageType.PriceAccept => "[Price Accept]",
            MessageType.OfferApproval => "[Offer Approval]",
            _ => string.Empty
        };
    }
}
