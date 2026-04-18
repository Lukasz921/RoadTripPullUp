using Application.Interfaces.Trip;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TripRequestRepository : ITripRequestRepository
{
    private readonly AppDbContext _context;

    public TripRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task Save(TripRequest request)
    {
        var exists = await _context.TripRequest.AnyAsync(r => r.Id == request.Id);
        if (!exists)
        {
            _context.TripRequest.Add(request);
        }
        else
        {
            _context.TripRequest.Update(request);
        }
        await _context.SaveChangesAsync();
    }

    public async Task<TripRequest?> GetById(Guid id)
    {
        return await _context.TripRequest.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<TripRequest>> GetByTripId(Guid tripId)
    {
        return await _context.TripRequest
            .Where(r => r.TripId == tripId)
            .ToListAsync();
    }
}
