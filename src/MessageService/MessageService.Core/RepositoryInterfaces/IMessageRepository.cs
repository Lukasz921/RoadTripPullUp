using MessageService.Core.Models;

namespace MessageService.Core.RepositoryInterfaces;

public interface IMessageRepository
{
    Task<Message> CreateAsync(Message message);
    Task<List<Message>> GetForConversationAsync(Guid conversationId, int skip, int take);
    Task<List<Message>> GetForUserSinceAsync(Guid userId, DateTime since);
    Task<Message?> GetByIdAsync(Guid id);
    Task MarkMessagesReadAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid readerId, DateTime readAt);
}
