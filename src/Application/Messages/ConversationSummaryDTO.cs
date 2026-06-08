namespace Application.Messages;

public class ConversationSummaryDTO
{
    public Guid PartnerId { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public string LastTimestamp { get; set; } = string.Empty;
}
