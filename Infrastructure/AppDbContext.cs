using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<Route> Routes { get; set; }
    public DbSet<TripRequest> TripRequest { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired();
            entity.Property(u => u.Surname).IsRequired();
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
        });

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

            // Trip -> Driver (wiele tripów może mieć jednego drivera)
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trip -> Route
            entity.HasOne<Route>()
                .WithMany()
                .HasForeignKey(t => t.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trip <-> Passengers (many-to-many)
            entity.HasMany(t => t.Passengers)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "TripPassengers",
                    j => j
                        .HasOne<User>()
                        .WithMany()
                        .HasForeignKey("PassengerId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Trip>()
                        .WithMany()
                        .HasForeignKey("TripId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("TripId", "PassengerId");
                        j.ToTable("TripPassengers");
                    });
        });

        modelBuilder.Entity<TripRequest>(entity =>
        {
            entity.HasKey(tr => tr.Id);

            entity.Property(tr => tr.TripRequestStatus).IsRequired();

            entity.HasOne<Trip>()
                .WithMany()
                .HasForeignKey(tr => tr.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(tr => tr.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

        });
    }

}