using System.Security.Claims;
using Application.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Messages;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessagingService _messagingService;

    public MessagesController(IMessagingService messagingService)
    {
        _messagingService = messagingService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MessageResponseDTO), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Send([FromBody] SendMessageDTO dto)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var senderId))
            return Unauthorized(new { message = "Missing or invalid user identifier in token." });

        var result = await _messagingService.SendMessage(senderId, dto);
        return Created($"/api/messages/{dto.ReceiverId}", result);
    }

    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<ConversationSummaryDTO>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetConversations()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Missing or invalid user identifier in token." });

        var conversations = await _messagingService.GetConversations(userId);
        return Ok(conversations);
    }

    [HttpGet("{receiverId:guid}")]
    [ProducesResponseType(typeof(List<MessageResponseDTO>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetConversation([FromRoute] Guid receiverId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Missing or invalid user identifier in token." });

        var messages = await _messagingService.GetConversation(userId, receiverId);
        return Ok(messages);
    }
}
