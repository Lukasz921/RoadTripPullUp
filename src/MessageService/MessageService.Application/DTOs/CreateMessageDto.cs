using System.Text.Json.Nodes;
using MessageService.Core.Models;

namespace MessageService.Application.DTOs;

public class CreateMessageDto
{
    public Guid ConversationId { get; set; }

    public MessageType Type { get; set; }
    public JsonObject? Payload { get; set; }
}
