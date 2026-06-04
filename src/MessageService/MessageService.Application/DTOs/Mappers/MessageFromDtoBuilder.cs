using MessageService.Application.Services;
using MessageService.Core.Models;

namespace MessageService.Application.DTOs.Mappers;

public class MessageFromDtoBuilder
{
    private readonly Message _message;
    
    public MessageFromDtoBuilder(CreateMessageDto createMessageDto, IClockService clockService)
    {
        _message = new Message
        {
            ConversationId = createMessageDto.ConversationId,
            Type = createMessageDto.Type,
            Payload = createMessageDto.Payload,
            CreatedAt = clockService.Now,
            Id = Guid.NewGuid()
        };
    }
    
    public MessageFromDtoBuilder WithSender(Guid senderId)
    {
        _message.SenderId = senderId;
        return this;
    }
    
    public bool ValidateType(ConversationType conversationType)
    {
        return conversationType == ConversationType.Direct
            ? _message.Type != MessageType.Location
            : _message.Type is not (MessageType.PriceOffer or MessageType.PriceAccept or MessageType.OfferApproval);
    }
    
    public Message Build() => _message;
}