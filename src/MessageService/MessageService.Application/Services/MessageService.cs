using MessageService.Application.DTOs;
using MessageService.Application.DTOs.Mappers;
using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;

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

    public async Task<Guid> CreateMessageAsync(CreateMessageDto dto, Guid senderId)
    {
        var conv = await _conversations.GetByIdAsync(dto.ConversationId);
        if (conv == null) throw new KeyNotFoundException("conversation not found");

        // validate sender is member
        if (conv.Members.All(m => m.UserId != senderId)) throw new UnauthorizedAccessException("sender not in conversation");

        // type restrictions per spec
        if (conv.Type == ConversationType.Direct)
        {
            if (dto.Type == MessageType.Location) throw new InvalidOperationException("LOCATION not allowed in DIRECT");
        }
        else
        {
            if(dto.Type is MessageType.PriceOffer or MessageType.PriceAccept or MessageType.OfferApproval)
                throw new InvalidOperationException("price negotiation not allowed in GROUP");
        }

        var msg = new Message
        {
            ConversationId = dto.ConversationId,
            SenderId = senderId,
            Type = dto.Type,
            Payload = dto.Payload,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _messages.CreateAsync(msg);

        // publish via notifier (redis + signalr)
        await _notifier.PublishMessageCreatedAsync(created);

        return created.Id;
        // TODO: convert into builder
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid conversationId, int skip, int take)
    {
        var messages = await _messages.GetForConversationAsync(conversationId, skip, take);
        return messages.Select(m => new MessageIntoDtoBuilder(m).Build());
    }

    public async Task<MessageDto?> GetByIdAsync(Guid messageId)
    {
        var m = await _messages.GetByIdAsync(messageId);
        return m == null ? null : new MessageIntoDtoBuilder(m).Build();
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
        return messages.Select(m => new MessageIntoDtoBuilder(m).Build());
    }
}
