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
    
    public bool ValidateType(string conversationType)
    {
        return conversationType == "direct"
            ? _message.Type != "location"
            : _message.Type is not ("priceOffer" or "priceAccept" or "offerApproval");
    }
    
    public Message Build() => _message;
}