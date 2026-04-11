using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TripRepository : ITripRepository
{
    private readonly AppDbContext _context;

    public TripRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task Save(Trip trip)
    {
        var exists = await _context.Trips.AnyAsync(t => t.Id == trip.Id);

        if (!exists)
        {
            _context.Trips.Add(trip);
        }
        else
        {
            _context.Trips.Update(trip);
        }

        await _context.SaveChangesAsync();
    }
}
