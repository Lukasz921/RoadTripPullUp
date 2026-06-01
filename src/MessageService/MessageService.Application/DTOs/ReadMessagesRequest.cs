namespace MessageService.Application.DTOs;

public class ReadMessagesRequest
{
    public List<Guid> MessageIds { get; set; } = [];
    public DateTime? ReadAt { get; set; }
}

