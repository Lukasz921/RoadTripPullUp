using MessageService.DTOs;
using MessageService.Models;
using MessageService.Repositories;

namespace MessageService.Services;

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
        if (!conv.Members.Any(m => m.UserId == creatorId))
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
        var convs = await _conversations.GetForUserAsync(userId, skip, take);
        return convs.Select(c => new ConversationDto
        {
            ConversationId = c.Id,
            IsGroup = c.IsGroup,
            Name = c.Title,
            Date = c.Date,
            Participants = c.Members.Select(m => m.UserId).ToList()
        });
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        return await _conversations.GetByIdAsync(id);
    }
}
