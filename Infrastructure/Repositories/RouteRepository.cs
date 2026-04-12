using Application.Interfaces.Trip;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RouteRepository : IRouteRepository
{
    private readonly AppDbContext _context;

    public RouteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task Save(Route route)
    {
        var exists = await _context.Routes.AnyAsync(r => r.Id == route.Id);

        if (!exists)
        {
            _context.Routes.Add(route);
        }
        else
        {
            _context.Routes.Update(route);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<Route> GetById(Guid id)
    {
        var route = await _context.Routes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return route!;
    }
}
