using MessageService.Application.DTOs;

namespace MessageService.Application.Services;

public interface IMessageService
{
    Task<Guid> CreateMessageAsync(Guid conversationId, CreateMessageDto dto, Guid senderId);
    Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid conversationId, int skip, int take);
    Task<IEnumerable<MessageDto>> SyncMessagesAsync(Guid userId, DateTime lastReceivedAt); // TODO: swap to DateTimeOffset
    Task<MessageDto?> GetByIdAsync(Guid messageId);
    Task MarkMessagesReadAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid readerId, DateTime readAt);
}
