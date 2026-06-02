using MessageService.Core.Models;
using MessageService.Core.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace MessageService.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _db;

    public MessageRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Message> CreateAsync(Message message)
    {
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }

    public async Task<List<Message>> GetForConversationAsync(Guid conversationId, int skip, int take)
    {
        return await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<Message>> GetForUserSinceAsync(Guid userId, DateTime since)
    {
        // messages in conversations where user is a member
        var convIds = await _db.ConversationMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => cm.ConversationId)
            .ToListAsync();

        return await _db.Messages
            .Where(m => convIds.Contains(m.ConversationId) && m.CreatedAt > since)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        return await _db.Messages.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task MarkMessagesReadAsync(Guid conversationId, IEnumerable<Guid> messageIds, Guid readerId, DateTime readAt)
    {
        // ensure messages belong to conversation
        var msgs = await _db.Messages.Where(m => messageIds.Contains(m.Id) && m.ConversationId == conversationId).ToListAsync();

        foreach (var m in msgs)
        {
            var exists = await _db.MessageReads.AnyAsync(r => r.MessageId == m.Id && r.ReaderId == readerId);
            if (!exists)
            {
                _db.MessageReads.Add(new MessageRead
                {
                    MessageId = m.Id,
                    ReaderId = readerId,
                    ReadAt = readAt
                });
            }
        }

        await _db.SaveChangesAsync();
    }
}
