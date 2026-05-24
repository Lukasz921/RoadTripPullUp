using MessageService.Application.Common;
using MessageService.Application.DTOs;
using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;

namespace MessageService.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversations;
    private readonly IUserRepository _users;
    private readonly IClock _clock;

    public ConversationService(IConversationRepository conversations, IUserRepository users, IClock clock)
    {
        _conversations = conversations;
        _users = users;
        _clock = clock;
    }

    public async Task<Guid> CreateConversationAsync(CreateConversationDto dto, Guid creatorId)
    {
        // basic validation
        if (dto.Participants == null || dto.Participants.Count == 0)
            throw new ArgumentException("participants required");

        var conv = new Conversation
        {
            Type = dto.Type,
            Title = dto.Title,
            Date = dto.Date,
            CreatedAt = _clock.Now
        };

        foreach (var p in dto.Participants.Distinct())
        {
            conv.Members.Add(new ConversationMember
            {
                UserId = p,
                JoinedAt = _clock.Now,
                Role = 0
            });
        }

        // ensure creator is member
        if (conv.Members.All(m => m.UserId != creatorId))
        {
            conv.Members.Add(new ConversationMember
            {
                UserId = creatorId,
                JoinedAt = _clock.Now,
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
            Type = tuple.conversation.Type,
            Name = tuple.conversation.Title,
            Date = tuple.conversation.Date,
            Participants = tuple.conversation.Members.Select(m => m.UserId).ToList(),
            LastMessageId = tuple.lastMessage?.Id ?? Guid.Empty,
            LastMessagePreview = GetMessagePreview(tuple.lastMessage),
            LastMessageCreatedAt = tuple.lastMessage?.CreatedAt ?? DateTime.UnixEpoch
        });
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        return await _conversations.GetByIdAsync(id);
    }
    
    private static string GetMessagePreview(Message? msg) // TODO: move to a helper/extension method
    {
        if (msg == null) return string.Empty;

        return msg.Type switch
        {
            MessageType.Text => msg.Payload?["text"]?.ToString() ?? string.Empty,
            MessageType.Location => "[Location]",
            MessageType.PriceOffer => "[Price Offer]",
            MessageType.PriceAccept => "[Price Accept]",
            MessageType.OfferApproval => "[Offer Approval]",
            _ => string.Empty
        };
    }
}
