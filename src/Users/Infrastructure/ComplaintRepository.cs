using Microsoft.EntityFrameworkCore;
using Users.Application.Interfaces;
using Users.Core;

namespace Users.Infrastructure;

public class ComplaintRepository : IComplaintRepository
{
    private readonly UsersDbContext _context;

    public ComplaintRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task Save(Complaint complaint)
    {
        if (await _context.Complaints.AnyAsync(c => c.Id == complaint.Id))
        {
            _context.Complaints.Update(complaint);
        }
        else
        {
            await _context.Complaints.AddAsync(complaint);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<Complaint?> FindById(Guid id)
    {
        return await _context.Complaints.FindAsync(id);
    }

    public async Task<(List<Complaint> Items, int TotalCount)> GetAll(int page, int pageSize)
    {
        var query = _context.Complaints.OrderByDescending(c => c.CreatedAt);
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
