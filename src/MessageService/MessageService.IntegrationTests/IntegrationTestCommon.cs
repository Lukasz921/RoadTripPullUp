using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace MessageService.IntegrationTests;

public static class IntegrationTestCommon
{
    public static async Task<(Guid, Guid)> CreateConversation2Members(HttpClient client)
    {
        var tripId = Guid.NewGuid();
        var createConversationDto = new
        {
            TripId = tripId,
            Title = "Test Conversation",
            Date = DateTime.UtcNow,
            Participants = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };
        var response = await client.PostAsJsonAsync("/api/v1/message/conversations", createConversationDto);
        var parsedResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var conversationId = parsedResponse?["conversationId"] != null
            ? Guid.Parse(parsedResponse["conversationId"].ToString() ?? "")
            : Guid.Empty;
        return conversationId == Guid.Empty ? throw new Exception("Failed to create conversation for test") : (conversationId, tripId);
    }

    public static async Task<Guid> CreateMessage(Guid conversationId, HttpClient client)
    {
        var createMessageDto = new
        {
            ConversationId = conversationId,
            Payload = new JsonObject { ["text"] = "Hello, world!" },
            Type = "text"
        };
        
        var messageResponse = await client.PostAsJsonAsync("/api/v1/message/messages", createMessageDto);
        var response2 = await messageResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var messageId = response2?["messageId"] != null ? Guid.Parse(response2["messageId"].ToString()) : Guid.Empty;
        return messageId == Guid.Empty ? throw new Exception("Failed to create message for test") : messageId;
    }
}