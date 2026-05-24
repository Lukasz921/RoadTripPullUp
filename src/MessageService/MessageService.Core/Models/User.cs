namespace MessageService.Core.Models;

public class User
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}

