using MessageService.Application.DTOs;
using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;

namespace MessageService.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversations;
    private readonly IUserRepository _users;

    public ConversationService(IConversationRepository conversations, IUserRepository users)
    {
        _conversations = conversations;
        _users = users;
    }

    public async Task<Guid> CreateConversationAsync(CreateConversationDto dto, Guid creatorId)
    {
        // basic validation
        if (dto.Participants == null || dto.Participants.Count == 0)
            throw new ArgumentException("participants required");

        var conv = new Conversation
        {
            IsGroup = dto.IsGroup,
            Title = dto.Title,
            Date = dto.Date,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var p in dto.Participants.Distinct())
        {
            conv.Members.Add(new ConversationMember
            {
                UserId = p,
                JoinedAt = DateTime.UtcNow,
                Role = 0
            });
        }

        // ensure creator is member
        if (conv.Members.All(m => m.UserId != creatorId))
        {
            conv.Members.Add(new ConversationMember
            {
                UserId = creatorId,
                JoinedAt = DateTime.UtcNow,
                Role = 1
            });
        }

        var created = await _conversations.CreateAsync(conv);
        return created.Id;
    }

    public async Task<IEnumerable<ConversationDto>> GetForUserAsync(Guid userId, int skip, int take)
    {
        var items = await _conversations.GetForUserWithLastMessageAsync(userId, skip, take);
        return items.Select(tuple => new ConversationDto
        {
            ConversationId = tuple.conversation.Id,
            IsGroup = tuple.conversation.IsGroup,
            Name = tuple.conversation.Title,
            Date = tuple.conversation.Date,
            Participants = tuple.conversation.Members.Select(m => m.UserId).ToList(),
            LastMessage = tuple.lastMessage == null ? null : new LastMessageDto
            {
                MessageId = tuple.lastMessage.Id,
                SenderId = tuple.lastMessage.SenderId,
                Type = tuple.lastMessage.Type,
                Payload = tuple.lastMessage.Payload,
                CreatedAt = tuple.lastMessage.CreatedAt
            }
        });
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        return await _conversations.GetByIdAsync(id);
    }
}
