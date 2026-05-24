namespace MessageService.Application.DTOs;

public class CreateConversationDto
{
    public bool IsGroup { get; set; }
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public List<Guid> Participants { get; set; } = [];
}

