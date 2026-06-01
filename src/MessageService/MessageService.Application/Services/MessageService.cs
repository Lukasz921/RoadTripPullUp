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
    private readonly IClockService _clockService;

    public MessageService(IMessageRepository messages, IConversationRepository conversations, INotificationService notifier, IClockService clockService)
    {
        _messages = messages;
        _conversations = conversations;
        _notifier = notifier;
        _clockService = clockService;
    }

    public async Task<Guid> CreateMessageAsync(CreateMessageDto dto, Guid senderId)
    {
        var conv = await _conversations.GetByIdAsync(dto.ConversationId);
        if (conv == null) throw new KeyNotFoundException("conversation not found");

        // validate sender is member
        if (conv.Members.All(m => m.UserId != senderId)) throw new UnauthorizedAccessException("sender not in conversation");

        var messageBuilder = new MessageFromDtoBuilder(dto, _clockService).WithSender(senderId);
        if (!messageBuilder.ValidateType(conv.Type)) throw new InvalidOperationException("message type not allowed in conversation type");
        var msg = messageBuilder.Build();

        var created = await _messages.CreateAsync(msg);

        // publish via notifier (redis + signalr)
        await _notifier.PublishMessageCreatedAsync(created);

        return created.Id;
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
