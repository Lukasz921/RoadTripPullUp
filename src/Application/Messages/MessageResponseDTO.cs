namespace Application.Messages;

public class MessageResponseDTO
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}
