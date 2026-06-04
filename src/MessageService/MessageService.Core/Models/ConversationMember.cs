namespace MessageService.Core.Models;

public class ConversationMember
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid UserId
    {
        get => User?.Id ?? Guid.Empty;
        set
        {
            if (User != null)
                User.Id = value;
        }
    } // TODO: check the logic further
    public User? User { get; set; }

    public int Role { get; set; } // TODO: what is this for? do we need it?
    public DateTime JoinedAt { get; set; }
}

