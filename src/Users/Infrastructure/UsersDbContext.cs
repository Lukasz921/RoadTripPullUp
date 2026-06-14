using Microsoft.EntityFrameworkCore;
using Users.Core;

namespace Users.Infrastructure;

public class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Surname).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.DateOfBirth).IsRequired();
            entity.Property(u => u.AvgRating).HasDefaultValue(0);
            entity.Property(u => u.RatingsCount).HasDefaultValue(0);
            entity.Property(u => u.IsBanned).HasDefaultValue(false);
            entity.Property(u => u.BanReason).HasMaxLength(500);
            entity.Property(u => u.BannedUntil).IsRequired(false);
        });
    }
}
