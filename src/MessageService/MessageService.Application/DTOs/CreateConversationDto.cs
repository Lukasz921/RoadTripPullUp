using MessageService.Core.Models;

namespace MessageService.Application.DTOs;

public class CreateConversationDto
{
    public ConversationType Type { get; set; } // TODO: Consider using an enum for conversation type
    public string? Title { get; set; }
    public DateTime? Date { get; set; }
    public List<Guid> Participants { get; set; } = [];
}