using MessageService.Core.Models;

namespace MessageService.Application.DTOs.Mappers;

public static class ConversationDtoMapper
{
    public static ConversationDto ToConversationDto(this Conversation conversation)
    {
        return new ConversationDto
        {
            ConversationId = conversation.Id,
            Type = conversation.Type,
            TripId = conversation.TripId,
            Name = conversation.Title,
            Date = conversation.Date,
            Participants = conversation.Members.Select(m => m.UserId).ToList(),
            // TODO: set LastMessageId, LastMessagePreview, LastMessageCreatedAt, UnreadMessagesCount
        };
    }
}