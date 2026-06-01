using MessageService.Core.Models;

namespace MessageService.Application.DTOs.Mappers;

public class ConversationFromDtoBuilder
{
    private readonly Conversation _conversation;
    
    public ConversationFromDtoBuilder(CreateConversationDto createConversationDto)
    {
        _conversation = new Conversation
        {
            Type = createConversationDto.Type,
            TripId = createConversationDto.TripId,
            Title = createConversationDto.Title,
            Date = createConversationDto.Date,
        };
    }
    
    public Conversation Build() => _conversation;
}