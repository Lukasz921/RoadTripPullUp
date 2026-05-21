using Application.Messages;
using Core.Messages;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Messages;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Message>> GetConversationAsync(Guid userId1, Guid userId2)
    {
        return await _context.Messages
            .Where(m =>
                (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<List<Message>> GetConversationsAsync(Guid userId)
    {
        var messages = await _context.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();

        return messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => g.First())
            .ToList();
    }
}
