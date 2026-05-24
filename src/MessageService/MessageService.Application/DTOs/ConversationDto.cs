using MessageService.Core.Models;

namespace MessageService.Application.DTOs;

public class ConversationDto
{
    public Guid ConversationId { get; set; }
    public ConversationType Type { get; set; }
    public string? Name { get; set; }
    public DateTime? Date { get; set; }
    public List<Guid> Participants { get; set; } = [];
    public Guid LastMessageId { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageCreatedAt { get; set; } 
    // public int UnreadMessagesCount { get; set; } // TODO: not implemented
}
