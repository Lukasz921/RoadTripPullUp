using System.Security.Claims;
using Application.Exceptions;
using MessageService.Application.DTOs;
using MessageService.Application.Helpers;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/v1/message")]
[Authorize]
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
        // TODO: add validation that the message type is text
        // Currently only text messages are supported
        var userId = GetUserId();
        if (dto.ConversationId == Guid.Empty) throw new InvalidParametersException("conversationId is required");

        var id = await _messages.CreateMessageAsync(dto, userId);
        return CreatedAtAction(nameof(Get), new { conversationId = dto.ConversationId, messageId = id }, new { messageId = id });
    }

    // GET /api/v1/message/conversations/{conversationId}/messages
    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> List(Guid conversationId,
        [FromQuery] int? fromConversation = null,
        [FromQuery] int? toConversation = null)
    {
        FromToIntoSkipTake.Convert(fromConversation, toConversation, out var skip, out var take);
        
        var list = await _messages.GetMessagesAsync(conversationId, skip, take);
        return Ok(list);
    }

    // GET /api/v1/message/messages/{messageId}
    [HttpGet("messages/{messageId:guid}")]
    public async Task<IActionResult> Get(Guid messageId)
    {
        var m = await _messages.GetByIdAsync(messageId);
        return m == null ? throw new NotFoundException("Message not found") : Ok(m);
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
            if (m.ConversationId != req.ConversationId) throw new InvalidParametersException("Message with given id does not belong to conversation with given id");

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

        throw new InvalidParametersException("Provide either LastReadMessageId or LastReadTimestamp");
        
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(sub) ? Guid.Empty : Guid.Parse(sub);
    }
}
