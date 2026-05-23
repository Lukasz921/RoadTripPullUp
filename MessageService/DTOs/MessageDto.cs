using System;
using System.Text.Json.Nodes;

namespace MessageService.DTOs;

public class MessageDto
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Type { get; set; } = "TEXT";
    public JsonObject? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
}

