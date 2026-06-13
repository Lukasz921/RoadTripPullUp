using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MessageService.Core.Models;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public Guid SenderId { get; set; }
    public string Type { get; set; } = "text";
    public JsonObject? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
}