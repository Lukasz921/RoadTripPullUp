using Application.DTOs.Messaging;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Messaging;
using Core.Entities;

namespace Application.Services;

public class MessagingService : IMessagingService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;

    public MessagingService(IMessageRepository messageRepository, IUserRepository userRepository)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
    }

    public async Task<MessageResponseDTO> SendMessage(Guid senderId, SendMessageDTO dto)
    {
        var receiver = await _userRepository.FindById(dto.ReceiverId);
        if (receiver == null)
            throw new NotFoundException($"User with id {dto.ReceiverId} not found.");

        var message = Message.Create(senderId, dto.ReceiverId, dto.Content);
        await _messageRepository.SaveAsync(message);

        return MapToDTO(message);
    }

    public async Task<List<MessageResponseDTO>> GetConversation(Guid userId1, Guid userId2)
    {
        var messages = await _messageRepository.GetConversationAsync(userId1, userId2);
        return messages.Select(MapToDTO).ToList();
    }

    public async Task<List<ConversationSummaryDTO>> GetConversations(Guid userId)
    {
        var latestMessages = await _messageRepository.GetConversationsAsync(userId);
        return latestMessages.Select(m => new ConversationSummaryDTO
        {
            PartnerId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
            LastMessage = m.Content,
            LastTimestamp = m.Timestamp.ToString("o")
        }).ToList();
    }

    private static MessageResponseDTO MapToDTO(Message m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        ReceiverId = m.ReceiverId,
        Content = m.Content,
        Timestamp = m.Timestamp.ToString("o"),
        IsRead = m.IsRead
    };
}
