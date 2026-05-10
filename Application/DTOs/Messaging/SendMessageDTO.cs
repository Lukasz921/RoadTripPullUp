namespace Application.DTOs.Messaging;

public class SendMessageDTO
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}
