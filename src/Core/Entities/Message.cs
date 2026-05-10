namespace Core.Entities;

public class Message
{
    public Guid Id { get; private set; }
    public Guid SenderId { get; private set; }
    public Guid ReceiverId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public bool IsRead { get; set; }

    private Message() { }

    public static Message Create(Guid senderId, Guid receiverId, string content)
    {
        if (senderId == Guid.Empty)
            throw new ArgumentException("SenderId cannot be empty.", nameof(senderId));
        if (receiverId == Guid.Empty)
            throw new ArgumentException("ReceiverId cannot be empty.", nameof(receiverId));
        if (senderId == receiverId)
            throw new ArgumentException("Cannot send a message to yourself.");
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty.", nameof(content));
        if (content.Length > 1000)
            throw new ArgumentException("Message content cannot exceed 1000 characters.", nameof(content));

        return new Message
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content.Trim(),
            Timestamp = DateTime.UtcNow,
            IsRead = false
        };
    }
}
