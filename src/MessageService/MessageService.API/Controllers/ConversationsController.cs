using System.Security.Claims;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/conversations")]
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
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var skip = (page - 1) * pageSize;
        var convs = await _conversations.GetForUserAsync(userId, skip, pageSize);
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

        var dto = new ConversationDto
        {
            ConversationId = conv.Id,
            Type = conv.Type,
            Name = conv.Title,
            Date = conv.Date,
            Participants = conv.Members.Select(m => m.UserId).ToList()
        };

        return Ok(dto);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(sub) ? Guid.Empty : Guid.Parse(sub);
    }
}
