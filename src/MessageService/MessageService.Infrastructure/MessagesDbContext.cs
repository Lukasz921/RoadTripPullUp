using MessageService.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageService.Infrastructure;

public class MessagesDbContext : DbContext
{
    public MessagesDbContext(DbContextOptions<MessagesDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageRead> MessageReads => Set<MessageRead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            b.Property(u => u.DisplayName).HasColumnName("display_name");
            b.Property(u => u.Username).HasColumnName("username");
            b.Property(u => u.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Conversation>(b =>
        {
            b.ToTable("conversations");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            b.Property(c => c.Type).HasColumnName("type");
            b.Property(c => c.Title).HasColumnName("title");
            b.Property(c => c.Date).HasColumnName("date");
            b.Property(c => c.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<ConversationMember>(b =>
        {
            b.ToTable("conversation_members");
            b.HasKey(cm => new { cm.ConversationId, cm.UserId });
            b.Property(cm => cm.ConversationId).HasColumnName("conversation_id");
            b.Property(cm => cm.UserId).HasColumnName("user_id");
            b.Property(cm => cm.Role).HasColumnName("role");
            b.HasOne(cm => cm.Conversation).WithMany(c => c.Members).HasForeignKey(cm => cm.ConversationId);
            b.HasOne(cm => cm.User).WithMany().HasForeignKey(cm => cm.UserId);
        });

        modelBuilder.Entity<Message>(b =>
        {
            b.ToTable("messages");
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            b.Property(m => m.ConversationId).HasColumnName("conversation_id");
            b.Property(m => m.SenderId).HasColumnName("sender_id");
            b.Property(m => m.Type).HasColumnName("type");
            b.Property(m => m.Payload).HasColumnName("payload").HasColumnType("jsonb");
            b.Property(m => m.CreatedAt).HasColumnName("created_at");

            b.HasOne(m => m.Conversation).WithMany(c => c.Messages).HasForeignKey(m => m.ConversationId);
        });

        modelBuilder.Entity<MessageRead>(b =>
        {
            b.ToTable("message_reads");
            b.HasKey(mr => new { mr.MessageId, mr.ReaderId });
            b.Property(mr => mr.MessageId).HasColumnName("message_id");
            b.Property(mr => mr.ReaderId).HasColumnName("reader_id");
            b.Property(mr => mr.ReadAt).HasColumnName("read_at");
            b.HasOne(mr => mr.Message).WithMany().HasForeignKey(mr => mr.MessageId);
        });

        // Indexes
        modelBuilder.Entity<Message>().HasIndex(m => new { m.ConversationId, m.CreatedAt });
        modelBuilder.Entity<ConversationMember>().HasIndex(cm => cm.UserId);
        modelBuilder.Entity<MessageRead>().HasIndex(mr => mr.ReaderId);
    }
}
