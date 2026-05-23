using Core.Messages;

namespace Application.Messages;

public interface IMessageRepository
{
    Task SaveAsync(Message message);
    Task<List<Message>> GetConversationAsync(Guid userId1, Guid userId2);
    Task<List<Message>> GetConversationsAsync(Guid userId);
}
