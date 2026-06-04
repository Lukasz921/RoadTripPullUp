using MessageService.Application.DTOs;
using MessageService.Core.Models;

namespace MessageService.Application.Services;

public interface IConversationService
{
    Task<Guid> CreateConversationAsync(CreateConversationDto dto, Guid creatorId);
    Task<IEnumerable<ConversationDto>> GetForUserAsync(Guid userId, int skip, int take);
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<Conversation?> GetGroupForTripAsync(Guid tripId);
    Task<List<Conversation>> GetDirectForTripAsync(Guid tripId, Guid userId);
    Task JoinConversationAsync(Guid conversationId, Guid userId);
}
