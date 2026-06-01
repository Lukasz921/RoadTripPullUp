using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MessageService.Application.Services;
using MessageService.Application.DTOs;
using System.Security.Claims;

namespace MessageService.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IClockService _clockService;

    public ChatHub(IMessageService messageService, IClockService clockService)
    {
        _messageService = messageService;
        _clockService = clockService;
    }

    public Task JoinConversation(string conversationId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    public Task LeaveConversation(string conversationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
    }

    /// <summary>
    /// Client calls this to create/send a message to a conversation.
    /// The method will call the application message service which persists the message
    /// and publishes notifications (including SignalR via Redis notifier).
    /// We return the created message id to the caller and also broadcast a lightweight
    /// confirmation to the conversation group to enable optimistic UI updates.
    /// </summary>
    public async Task<object> SendMessage(CreateMessageDto dto)
    {
        // obtain sender id from authenticated user claims
        var sub = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var senderId))
        {
            throw new HubException("Unauthorized: missing or invalid user identifier.");
        }

        try
        {
            var messageId = await _messageService.CreateMessageAsync(dto, senderId);

            // Broadcast to the group that a message was created. The RedisNotificationService
            // already publishes to SignalR as well, but broadcasting here gives immediate feedback
            // to clients connected directly to this instance.
            var payload = new
            {
                eventType = "message.created",
                data = new
                {
                    id = messageId,
                    conversationId = dto.ConversationId,
                    senderId,
                    type = dto.Type,
                    payload = dto.Payload,
                    createdAt = _clockService.Now
                }
            };

            await Clients.Group(dto.ConversationId.ToString()).SendAsync("MessageCreated", payload);

            return new { messageId };
        }
        catch (KeyNotFoundException knf)
        {
            throw new HubException("NotFound: " + knf.Message);
        }
        catch (UnauthorizedAccessException ua)
        {
            throw new HubException("Forbidden: " + ua.Message);
        }
        catch (InvalidOperationException ioe)
        {
            throw new HubException("InvalidOperation: " + ioe.Message);
        }
    }
}
