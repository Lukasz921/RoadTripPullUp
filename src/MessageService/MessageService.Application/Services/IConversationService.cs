using MessageService.DTOs;
using MessageService.Models;

namespace MessageService.Services;

public interface IConversationService
{
    Task<Guid> CreateConversationAsync(CreateConversationDto dto, Guid creatorId);
    Task<IEnumerable<DTOs.ConversationDto>> GetForUserAsync(Guid userId, int skip, int take);
    Task<Conversation?> GetByIdAsync(Guid id);
}
