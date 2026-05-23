using Microsoft.AspNetCore.Mvc;
using MessageService.Services;
using MessageService.DTOs;
using System.Security.Claims;

namespace MessageService.Controllers;

[ApiController]
[Route("api/conversations/{conversationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messages;

    public MessagesController(IMessageService messages)
    {
        _messages = messages;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid conversationId, [FromBody] CreateMessageDto dto)
    {
        var userId = GetUserId();
        var id = await _messages.CreateMessageAsync(conversationId, dto, userId);
        return CreatedAtAction(nameof(Get), new { conversationId = conversationId, messageId = id }, new { messageId = id });
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var skip = (page - 1) * pageSize;
        var list = await _messages.GetMessagesAsync(conversationId, skip, pageSize);
        return Ok(list);
    }

    [HttpGet("{messageId}")]
    public async Task<IActionResult> Get(Guid conversationId, Guid messageId)
    {
        var m = await _messages.GetByIdAsync(messageId);
        if (m == null) return NotFound();

        if (m.ConversationId != conversationId) return BadRequest();

        // ensure user is member of conversation
        // we need to ensure membership; use messages service to fetch conversation membership via repository indirectly
        // naive approach: if message exists, allow (improve later)

        return Ok(m);
    }

    [HttpPost("read")]
    public async Task<IActionResult> MarkRead(Guid conversationId, [FromBody] ReadMessagesRequest req)
    {
        var userId = GetUserId();
        var readAt = req.ReadAt ?? DateTime.UtcNow;
        await _messages.MarkMessagesReadAsync(conversationId, req.MessageIds, userId, readAt);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(sub) ? Guid.Empty : Guid.Parse(sub);
    }
}

public class ReadMessagesRequest
{
    public List<Guid> MessageIds { get; set; } = new();
    public DateTime? ReadAt { get; set; }
}
