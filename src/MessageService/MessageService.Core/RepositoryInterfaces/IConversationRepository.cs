using MessageService.Core.Models;

namespace MessageService.Core.RepositoryInterfaces;

public interface IConversationRepository
{
    Task<Conversation> CreateAsync(Conversation conversation);
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<List<Conversation>> GetForUserAsync(Guid userId, int skip, int take);
    Task<List<(Conversation conversation, Message? lastMessage)>> GetForUserWithLastMessageAsync(Guid userId, int skip, int take);
    Task<Conversation?> GetGroupConversationForTripAsync(Guid tripId);
}
