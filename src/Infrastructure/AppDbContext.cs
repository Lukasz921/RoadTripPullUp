using Microsoft.EntityFrameworkCore;
using Core.Messages;
using Core.TripPlanner;

namespace Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<TripRequest> TripRequest { get; set; }
    public DbSet<Message> Messages { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.From).IsRequired();
            entity.Property(r => r.To).IsRequired();

            // can be issues with db different than postgress
            entity.Property(r => r.BetweenPoints)
                .HasColumnType("text[]");
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Price).IsRequired();
            entity.Property(t => t.Date).IsRequired();
            entity.Property(t => t.MaxPassengers).IsRequired();
            entity.Property(t => t.OfferStatus).IsRequired();

            entity.Property(t => t.RowVersion).IsRowVersion();

            // Trip -> Driver
            entity.Property(t => t.DriverId).IsRequired();

            // Trip -> Route
            entity.HasOne<Route>()
                .WithMany()
                .HasForeignKey(t => t.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trip -> Passengers (primitive collection)
            // On PostgreSQL this will be mapped to uuid[] by default if we don't specify ToTable.
            // But we can specify it to be a separate table if we want.
            // For now, let's just let it be.
        });

        modelBuilder.Entity<TripRequest>(entity =>
        {
            entity.HasKey(tr => tr.Id);

            entity.Property(tr => tr.TripRequestStatus).IsRequired();

            entity.HasOne<Trip>()
                .WithMany()
                .HasForeignKey(tr => tr.TripId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.Property(tr => tr.PassengerId).IsRequired();
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Content).IsRequired().HasMaxLength(1000);
            entity.Property(m => m.Timestamp).IsRequired();
            
            entity.Property(m => m.SenderId).IsRequired();
            entity.Property(m => m.ReceiverId).IsRequired();
        });
    }

}