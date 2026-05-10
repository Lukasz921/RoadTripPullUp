using Application.DTOs.Messaging;

namespace Application.Interfaces.Messaging;


public interface IMessagingService
{
    Task<MessageResponseDTO> SendMessage(Guid senderId, SendMessageDTO dto);
    Task<List<MessageResponseDTO>> GetConversation(Guid userId1, Guid userId2);
    Task<List<ConversationSummaryDTO>> GetConversations(Guid userId);
}
