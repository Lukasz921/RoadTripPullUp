namespace MessageService.Core.Models;

public class Conversation
{
    public Guid Id { get; set; }
    public bool IsGroup { get; set; }
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<ConversationMember> Members { get; set; } = [];
    public List<Message> Messages { get; set; } = [];
}

