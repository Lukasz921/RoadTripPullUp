using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace MessageService.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _db;

    public ConversationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();
        return conversation;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        return await _db.Conversations
            .Include(c => c.Members)
            .ThenInclude(cm => cm.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Conversation>> GetForUserAsync(Guid userId, int skip, int take)
    {
        var convIds = await _db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ConversationId)
            .ToListAsync();

        return await _db.Conversations
            .Where(c => convIds.Contains(c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<(Conversation conversation, Message? lastMessage)>> GetForUserWithLastMessageAsync(Guid userId, int skip, int take)
    {
        var convIds = await _db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ConversationId)
            .ToListAsync();

        var convs = await _db.Conversations
            .Where(c => convIds.Contains(c.Id))
            .Include(c => c.Members)
                .ThenInclude(cm => cm.User)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        // fetch last messages for the selected conversations only (optimize using selected conv ids)
        var selectedIds = convs.Select(c => c.Id).ToList();
        var lastMessages = await _db.Messages
            .Where(m => selectedIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.CreatedAt).FirstOrDefault())
            .ToListAsync();

        var lastByConv = lastMessages.Where(m => m != null).ToDictionary(m => m!.ConversationId, m => m!);

        var result = convs.Select(c => (c, lastByConv.GetValueOrDefault(c.Id))).ToList();
        return result;
    }
    
    public async Task<Conversation?> GetGroupConversationForTripAsync(Guid tripId)
    {
        return await _db.Conversations
            .Include(c => c.Members)
                .ThenInclude(cm => cm.User)
            .FirstOrDefaultAsync(c => c.TripId == tripId && c.Type == ConversationType.Group);
    }

    public async Task<List<Conversation>> GetDirectConversationsForTripAsync(Guid tripId, Guid userId)
    {
        var convIds = await _db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ConversationId)
            .ToListAsync();

        return await _db.Conversations
            .Where(c => convIds.Contains(c.Id) && c.TripId == tripId && c.Type == ConversationType.Direct)
            .Include(c => c.Members)
                .ThenInclude(cm => cm.User)
            .ToListAsync();
    }
}
