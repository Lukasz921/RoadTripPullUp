using System.Text.Json.Nodes;

namespace MessageService.Core.Models;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public Guid SenderId { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public JsonObject? Payload { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum MessageType
{
    Text,
    PriceOffer,
    PriceAccept,
    OfferApproval,
    Location
}