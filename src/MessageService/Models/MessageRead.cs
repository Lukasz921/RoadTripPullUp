using System;

namespace MessageService.Models;

public class MessageRead
{
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public Guid ReaderId { get; set; }

    public DateTime ReadAt { get; set; }
}

