namespace Users.Core;

public class Rating
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RaterId { get; set; }
    public int Value { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties (optional, but good for EF)
    public User? User { get; set; }
    public User? Rater { get; set; }
}
