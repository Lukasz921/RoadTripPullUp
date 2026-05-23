using Microsoft.EntityFrameworkCore;
using MessageService.Infrastructure;
using MessageService.Models;

namespace MessageService.Repositories;

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
}

