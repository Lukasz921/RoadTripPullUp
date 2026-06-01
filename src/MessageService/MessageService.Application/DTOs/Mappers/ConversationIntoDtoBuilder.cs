using MessageService.Application.Helpers;
using MessageService.Core.Models;

namespace MessageService.Application.DTOs.Mappers;

public class ConversationIntoDtoBuilder
{
    private readonly ConversationDto _conversationDto;

    public ConversationIntoDtoBuilder(Conversation conversation)
    {
        _conversationDto = new ConversationDto
        {
            ConversationId = conversation.Id,
            Type = conversation.Type,
            TripId = conversation.TripId,
            Name = conversation.Title,
            Date = conversation.Date,
            Participants = conversation.Members.Select(m => m.UserId).ToList(),
        };
    }
    
    public ConversationIntoDtoBuilder WithLastMessage(Message message)
    {
        _conversationDto.LastMessageId = message.Id;
        _conversationDto.LastMessagePreview = message.GetMessagePreview();
        _conversationDto.LastMessageCreatedAt = message.CreatedAt;
        return this;
    }
    
    public ConversationDto Build()
    {
        return _conversationDto;
    }
}