using System;
using System.Collections.Generic;

namespace MessageService.DTOs;

public class ConversationDto
{
    public Guid ConversationId { get; set; }
    public bool IsGroup { get; set; }
    public string? Name { get; set; }
    public DateTime? Date { get; set; }
    public List<Guid> Participants { get; set; } = new();
}

