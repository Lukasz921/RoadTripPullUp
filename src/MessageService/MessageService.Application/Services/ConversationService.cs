using MessageService.Application.DTOs;
using MessageService.Application.DTOs.Mappers;
using MessageService.Application.Helpers;
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
