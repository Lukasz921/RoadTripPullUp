using System.Security.Claims;
using MessageService.Application.DTOs;
using MessageService.Application.Helpers;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Http;
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

        var id = await _messages.CreateMessageAsync(dto, userId);
        return CreatedAtAction(nameof(Get), new { conversationId = dto.ConversationId, messageId = id }, new { messageId = id });
    }

    // GET /api/v1/message/conversations/{conversationId}/messages
    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> List(Guid conversationId,
        [FromQuery] int? fromConversation = null,
        [FromQuery] int? toConversation = null)
    {
        int skip, take;

        try
        {
            FromToIntoSkipTake.Convert(fromConversation, toConversation, out skip, out take);
        }
        catch (BadHttpRequestException e)
        {
            return BadRequest(e.Message);
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
        // TODO: work on this
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
