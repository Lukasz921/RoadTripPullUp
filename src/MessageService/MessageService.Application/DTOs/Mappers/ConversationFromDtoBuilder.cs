using MessageService.Application.Services;
using MessageService.Core.Models;

namespace MessageService.Application.DTOs.Mappers;

public class ConversationFromDtoBuilder
{
    private readonly Conversation _conversation;
    private readonly IClockService _timeProvider;
    
    public ConversationFromDtoBuilder(CreateConversationDto createConversationDto, IClockService timeProvider)
    {
        _conversation = new Conversation
        {
            Type = createConversationDto.Type,
            TripId = createConversationDto.TripId,
            Title = createConversationDto.Title,
            Date = createConversationDto.Date,
        };
        _timeProvider = timeProvider;
    }
    
    public ConversationFromDtoBuilder WithMembers(List<Guid> participantIds)
    {
        _conversation.Members = participantIds.Distinct().Select(id => new ConversationMember
        {
            UserId = id,
            JoinedAt = _timeProvider.Now,
            Role = 0
        }).ToList();
        return this;
    }
    
    public ConversationFromDtoBuilder CheckDefaultMember(Guid creatorId)
    {
        if (_conversation.Members.All(m => m.UserId != creatorId))
        {
            _conversation.Members.Add(new ConversationMember
            {
                UserId = creatorId,
                JoinedAt = _timeProvider.Now,
                Role = 1
            });
        }

        return this;
    }
    
    public Conversation Build() => _conversation;
    
}