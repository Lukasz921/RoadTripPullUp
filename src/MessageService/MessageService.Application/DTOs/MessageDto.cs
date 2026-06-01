using System.Text.Json.Nodes;
using MessageService.Core.Models;

namespace MessageService.Application.DTOs;

public class MessageDto
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public MessageType Type { get; set; }
    public JsonObject? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
}

