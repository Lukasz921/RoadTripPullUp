namespace MessageService.Application.DTOs;

public class ReadReceiptRequest
{
    public Guid ConversationId { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTime? LastReadTimestamp { get; set; }
}

// TODO: niepodłączone do logiki systemu - podłączyć