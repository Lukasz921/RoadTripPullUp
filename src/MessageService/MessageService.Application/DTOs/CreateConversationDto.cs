namespace MessageService.Application.DTOs;

public class CreateConversationDto
{
    public Guid TripId { get; set; }
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public List<Guid> Participants { get; set; } = [];
}