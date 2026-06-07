using MessageService.Core.Models;

namespace MessageService.Application.Helpers;

public static class MessagePreview
{
    public static string GetMessagePreview(this Message msg)
    {
        return msg.Type switch
        {
            MessageType.Text => msg.Payload?["text"]?.ToString() ?? string.Empty,
            MessageType.Location => "[Location]",
            MessageType.PriceOffer => "[Price Offer]",
            MessageType.PriceAccept => "[Price Accept]",
            MessageType.OfferApproval => "[Offer Approval]",
            _ => string.Empty
        };
    }
}