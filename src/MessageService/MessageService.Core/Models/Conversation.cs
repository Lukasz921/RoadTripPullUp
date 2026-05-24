namespace MessageService.Core.Models;

public class Conversation
{
    public Guid Id { get; set; }
    public ConversationType Type { get; set; }
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<ConversationMember> Members { get; set; } = [];
    public List<Message> Messages { get; set; } = [];
}

public enum ConversationType
{
    Direct,
    Group
}