namespace MessageService.Application.DTOs;

public class LastMessageDto
{
    public Guid MessageId { get; set; }
    public Guid SenderId { get; set; }
    public string? Type { get; set; }
    public System.Text.Json.Nodes.JsonObject? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationDto
{
    public Guid ConversationId { get; set; }
    public bool IsGroup { get; set; }
    public string? Name { get; set; }
    public DateTime? Date { get; set; }
    public List<Guid> Participants { get; set; } = [];
    public LastMessageDto? LastMessage { get; set; }
}
