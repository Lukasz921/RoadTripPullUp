using System.Security.Claims;
using Application.Exceptions;
using MessageService.Application.DTOs;
using MessageService.Application.DTOs.Mappers;
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

        FromToIntoSkipTake.Convert(fromConversation, toConversation, out var skip, out var take);

        // updatedAfter is currently not used by the repository's GetForUserAsync signature; keep it for future use
        var convs = await _conversations.GetForUserAsync(userId, skip, take);
        return Ok(convs);
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<IActionResult> Get(Guid conversationId)
    {
        var conv = await _conversations.GetByIdAsync(conversationId);
        if (conv == null) throw new NotFoundException("Conversation with given id not found");

        // authorization: ensure current user is member
        var userId = GetUserId();
        if (conv.Members.All(m => m.UserId != userId))
            throw new ForbiddenException("User is not member of the conversation");
        
        var dto = new ConversationIntoDtoBuilder(conv)
            .WithLastMessage(conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
            .Build();
        return Ok(dto);
    }

    [HttpGet("byTripId/group/{tripId:guid}")]
    public async Task<IActionResult> GetGroupForTrip(Guid tripId)
    {
        var conv = await _conversations.GetGroupForTripAsync(tripId);
        var userId = GetUserId();
        if (conv == null) throw new NotFoundException("Conversation with given id not found");
        if (conv.Members.All(m => m.UserId != userId)) throw new ForbiddenException("User is not member of the conversation");
        var dto = new ConversationIntoDtoBuilder(conv)
            .WithLastMessage(conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
            .Build();
        return Ok(dto);
    }
    
    [HttpGet("/byTripId/direct/{tripId:guid}")]
    public async Task<IActionResult> GetDirectForTrip(Guid tripId)
    {
        var userId = GetUserId();
        var convs = await _conversations.GetDirectForTripAsync(tripId, userId);
        var dtos = convs.Select(conv => new ConversationIntoDtoBuilder(conv)
            .WithLastMessage(conv.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
            .Build());
        return Ok(dtos);
    }

    [HttpPost("{conversationId:guid}/join/{userId:guid}")]
    public async Task<IActionResult> Join(Guid conversationId, Guid userId)
    {
        var conv = await _conversations.GetByIdAsync(conversationId);
        if (conv == null) return NotFound();
        
        var callerId = GetUserId();
        if (conv.Members.All(m => m.UserId != callerId)) return Forbid();
        if (conv.Members.Any(m => m.UserId == userId)) return BadRequest(new { error = "already a member" });

        await _conversations.AddMemberAsync(conversationId, userId);
        return NoContent();
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
