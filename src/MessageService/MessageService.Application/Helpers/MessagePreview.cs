using MessageService.Core.Models;

namespace MessageService.Application.Helpers;

public static class MessagePreview
{
    public static string GetMessagePreview(this Message msg)
    {
        return msg.Type switch
        {
            "text" => msg.Payload?["text"]?.ToString() ?? string.Empty,
            "location" => "[Location]",
            "priceOffer" => "[Price Offer]",
            "priceAccept" => "[Price Accept]",
            "offerApproval" => "[Offer Approval]",
            _ => string.Empty
        };
    }
}