using MessageService.Application.DTOs;
using MessageService.Core.Models;

namespace MessageService.Application.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messages;
    private readonly IConversationRepository _conversations;
    private readonly INotificationService _notifier;

    public MessageService(IMessageRepository messages, IConversationRepository conversations, INotificationService notifier)
    {
        _messages = messages;
        _conversations = conversations;
        _notifier = notifier;
    }

    public async Task<Guid> CreateMessageAsync(Guid conversationId, CreateMessageDto dto, Guid senderId)
    {
        var conv = await _conversations.GetByIdAsync(conversationId);
        if (conv == null) throw new KeyNotFoundException("conversation not found");

        // validate sender is member
        if (!conv.Members.Any(m => m.UserId == senderId)) throw new UnauthorizedAccessException("sender not in conversation");

        // type restrictions per spec
        if (!conv.IsGroup)
        {
            if (dto.Type == "LOCATION") throw new InvalidOperationException("LOCATION not allowed in DIRECT");
        }
        else
        {
            if (dto.Type == "PRICE_OFFER" || dto.Type == "PRICE_ACCEPT" || dto.Type == "OFFER_APPROVAL")
                throw new InvalidOperationException("price negotiation not allowed in GROUP");
        }

        var msg = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Type = dto.Type,
            Payload = dto.Payload,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _messages.CreateAsync(msg);

        // publish via notifier (redis + signalr)
        await _notifier.PublishMessageCreatedAsync(created);

        return created.Id;
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid conversationId, int skip, int take)
    {
        var messages = await _messages.GetForConversationAsync(conversationId, skip, take);
        return messages.Select(m => new MessageDto
        {
            MessageId = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            Type = m.Type,
            Payload = m.Payload,
            CreatedAt = m.CreatedAt
        });
    }

    public async Task<MessageDto?> GetByIdAsync(Guid messageId)
    {
        var m = await _messages.GetByIdAsync(messageId);
        if (m == null) return null;
        return new MessageDto
        {
            MessageId = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            Type = m.Type,
            Payload = m.Payload,
            CreatedAt = m.CreatedAt
        };
    }

    public async Task MarkMessagesReadAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid readerId, DateTime readAt)
    {
        await _messages.MarkMessagesReadAsync(conversationId, messageIds, readerId, readAt);
        // publish read events
        await _notifier.PublishMessagesReadAsync(conversationId, messageIds, readerId, readAt);
    }

    public async Task<IEnumerable<MessageDto>> SyncMessagesAsync(Guid userId, DateTime lastReceivedAt)
    {
        var messages = await _messages.GetForUserSinceAsync(userId, lastReceivedAt);
        return messages.Select(m => new MessageDto
        {
            MessageId = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            Type = m.Type,
            Payload = m.Payload,
            CreatedAt = m.CreatedAt
        });
    }
}
