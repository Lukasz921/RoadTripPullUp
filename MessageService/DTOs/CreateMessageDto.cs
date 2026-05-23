using System.Text.Json.Nodes;

namespace MessageService.DTOs;

public class CreateMessageDto
{
    public string Type { get; set; } = "TEXT";
    public JsonObject? Payload { get; set; }
}

