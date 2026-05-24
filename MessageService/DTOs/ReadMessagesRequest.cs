using System;
using System.Collections.Generic;

namespace MessageService.DTOs;

public class ReadMessagesRequest
{
    public List<Guid> MessageIds { get; set; } = new();
    public DateTime? ReadAt { get; set; }
}

