using System.Text.Json.Serialization;

namespace MessageService.Core.Models;

public class Conversation
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public Guid TripId { get; set; }
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<ConversationMember> Members { get; set; } = [];
    public List<Message> Messages { get; set; } = [];
}