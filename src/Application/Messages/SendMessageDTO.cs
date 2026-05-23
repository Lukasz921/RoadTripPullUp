namespace Application.Messages;

public class SendMessageDTO
{
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
}
