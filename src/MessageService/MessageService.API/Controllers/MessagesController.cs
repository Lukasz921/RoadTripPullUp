using System.Security.Claims;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/v1/message")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messages;

    public MessagesController(IMessageService messages)
    {
        _messages = messages;
    }

    // POST /api/v1/message/messages
    [HttpPost("messages")]
    public async Task<IActionResult> Create([FromBody] CreateMessageDto dto)
    {
        var userId = GetUserId();
        if (dto.ConversationId == Guid.Empty)
            return BadRequest(new { error = "conversationId is required" });

        var id = await _messages.CreateMessageAsync(dto.ConversationId, dto, userId);
        return CreatedAtAction(nameof(Get), new { conversationId = dto.ConversationId, messageId = id }, new { messageId = id });
    }

    // GET /api/v1/message/conversations/{conversationId}/messages
    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> List(Guid conversationId,
        [FromQuery] int? fromConversation = null,
        [FromQuery] int? toConversation = null)
    {
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
        
        var list = await _messages.GetMessagesAsync(conversationId, skip, take);
        return Ok(list);
    }

    // GET /api/v1/message/messages/{messageId}
    [HttpGet("messages/{messageId:guid}")]
    public async Task<IActionResult> Get(Guid messageId)
    {
        var m = await _messages.GetByIdAsync(messageId);
        if (m == null) return NotFound();
        return Ok(m);
    }

    // GET /api/v1/message/messages/sync?lastReceivedAt=timestamp
    [HttpGet("messages/sync")]
    public async Task<IActionResult> Sync([FromQuery] DateTime? lastReceivedAt)
    {
        var userId = GetUserId();
        var since = lastReceivedAt ?? DateTime.UnixEpoch;
        var msgs = await _messages.SyncMessagesAsync(userId, since);
        return Ok(new { messages = msgs, serverTimestamp = DateTime.UtcNow });
    }

    // POST /api/v1/message/messages/read
    [HttpPost("messages/read")]
    public async Task<IActionResult> Read([FromBody] ReadReceiptRequest req)
    {
        if (req.ConversationId == Guid.Empty) return UnprocessableEntity(new { error = "conversationId is required" });

        var userId = GetUserId();

        if (req.LastReadMessageId != null)
        {
            // fetch the message to ensure it belongs to conversation
            var m = await _messages.GetByIdAsync(req.LastReadMessageId.Value);
            if (m == null) return NotFound();
            if (m.ConversationId != req.ConversationId) return BadRequest(new { error = "message does not belong to conversation" });

            // mark all messages up to this id as read: service expects list of ids, but we can pass a singleton for now
            // assume service implementation will interpret this correctly; otherwise it can be adapted later
            await _messages.MarkMessagesReadAsync(req.ConversationId, [req.LastReadMessageId.Value], userId, req.LastReadTimestamp ?? DateTime.UtcNow);
            return NoContent();
        }

        if (req.LastReadTimestamp != null)
        {
            // in this mock stage, we don't have a service method accepting timestamp; translate to current API by leaving messageIds empty
            await _messages.MarkMessagesReadAsync(req.ConversationId, [], userId, req.LastReadTimestamp.Value);
            return NoContent();
        }

        return UnprocessableEntity(new { error = "Provide either LastReadMessageId or LastReadTimestamp" });
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(sub) ? Guid.Empty : Guid.Parse(sub);
    }
}
