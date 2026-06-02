namespace MessageService.Core.Models;

public class ConversationMember
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int Role { get; set; }
    public DateTime JoinedAt { get; set; }
}

