using Application.Interfaces.Trip;
using Application.DTOs;
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

    public async Task<List<Trip>> Search(SearchTripsCriteria criteria)
    {
        var query = _context.Trips.AsNoTracking().AsQueryable();

        // Only public/active offers
        query = query.Where(t => t.OfferStatus == TripStatus.Active);

        if (!string.IsNullOrWhiteSpace(criteria.From))
        {
            var from = criteria.From.Trim();
            query = query.Where(t => _context.Routes.Any(r => r.Id == t.RouteId && r.From.ToLower() == from.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(criteria.To))
        {
            var to = criteria.To.Trim();
            query = query.Where(t => _context.Routes.Any(r => r.Id == t.RouteId && r.To.ToLower() == to.ToLower()));
        }

        if (criteria.Date != null)
        {
            var date = criteria.Date.Value.Date;
            var next = date.AddDays(1);
            query = query.Where(t => t.Date >= date && t.Date < next);
        }

        var results = await query.ToListAsync();

        return results;
    }
}
