using MessageService.Core.Models;

namespace MessageService.Application.DTOs.Mappers;

public class MessageIntoDtoBuilder
{
    private readonly MessageDto _messageDto;
    
    public MessageIntoDtoBuilder(Message message)
    {
        _messageDto = new MessageDto
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Type = message.Type,
            Payload = message.Payload,
            CreatedAt = message.CreatedAt
        };
    }
    
    public MessageDto Build() => _messageDto;
}