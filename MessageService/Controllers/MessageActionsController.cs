using Microsoft.AspNetCore.Mvc;
using MessageService.Services;
using System.Security.Claims;

namespace MessageService.Controllers;

[ApiController]
[Route("api/messages")]
public class MessageActionsController : ControllerBase
{
    private readonly IMessageService _messages;

    public MessageActionsController(IMessageService messages)
    {
        _messages = messages;
    }

    [HttpPost("{messageId}/read")]
    public async Task<IActionResult> MarkReadSingle(Guid messageId, [FromBody] SingleReadRequest req)
    {
        var userId = GetUserId();
        var m = await _messages.GetByIdAsync(messageId);
        if (m == null) return NotFound();
        var readAt = req.ReadAt ?? DateTime.UtcNow;
        await _messages.MarkMessagesReadAsync(m.ConversationId, new[] { messageId }, userId, readAt);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(sub) ? Guid.Empty : Guid.Parse(sub);
    }
}

public class SingleReadRequest
{
    public DateTime? ReadAt { get; set; }
}

