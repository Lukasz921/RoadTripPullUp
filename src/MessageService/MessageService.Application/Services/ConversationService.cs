using MessageService.Application.DTOs;
using MessageService.Application.DTOs.Mappers;
using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;

namespace MessageService.Application.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversations;
    private readonly IUserRepository _users;
    private readonly IClockService _clockService;

    public ConversationService(IConversationRepository conversations, IUserRepository users, IClockService clockService)
    {
        _conversations = conversations;
        _users = users;
        _clockService = clockService;
    }

    public async Task<Guid> CreateConversationAsync(CreateConversationDto dto, Guid creatorId)
    {
        // basic validation
        if (dto.Participants == null || dto.Participants.Count == 0)
            throw new ArgumentException("participants required");
        
        var conv = new ConversationFromDtoBuilder(dto, _clockService)
            .WithMembers(dto.Participants)
            .CheckDefaultMember(creatorId)
            .Build();

        var created = await _conversations.CreateAsync(conv);
        return created.Id;
    }

    public async Task JoinConversationAsync(Guid conversationId, Guid userId)
    {
        var conv = await _conversations.GetByIdAsync(conversationId);
        if (conv == null) return;
        var cm = new ConversationMember
        {
            UserId = userId,
            JoinedAt = _clockService.Now,
            Role = 0
        };
        conv.Members.Add(cm);
        await _conversations.AddUserToConversationAsync(cm);
    }

    public async Task<IEnumerable<ConversationDto>> GetForUserAsync(Guid userId, int skip, int take)
    {
        var items = await _conversations.GetForUserWithLastMessageAsync(userId, skip, take);
        return items.Select(tuple => new ConversationIntoDtoBuilder(tuple.conversation)
            .WithLastMessage(tuple.lastMessage)
            .Build());
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        return await _conversations.GetByIdAsync(id);
    }
    
    public async Task<Conversation?> GetGroupForTripAsync(Guid tripId)
    {
        return await _conversations.GetGroupConversationForTripAsync(tripId);
    }
    
    public async Task<List<Conversation>> GetDirectForTripAsync(Guid tripId, Guid userId)
    {
        return await _conversations.GetDirectConversationsForTripAsync(tripId, userId);
    }

    public Task AddMemberAsync(Guid conversationId, Guid userId) =>
        _conversations.AddMemberAsync(conversationId, userId, _clockService.Now);

    public async Task AddMemberToTripGroupAsync(Guid tripId, Guid userId)
    {
        var conv = await _conversations.GetGroupConversationForTripAsync(tripId);
        if (conv == null) return;
        await _conversations.AddMemberAsync(conv.Id, userId, _clockService.Now);
    }
}
