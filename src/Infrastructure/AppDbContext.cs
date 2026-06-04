using Microsoft.EntityFrameworkCore;
using Core.Messages;

namespace Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
