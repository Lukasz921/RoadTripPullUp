using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace MessageService.IntegrationTests;

public static class IntegrationTestCommon
{
    public static async Task<Guid> CreateConversation2Members(HttpClient client)
    {
        return await CreateConversationNMembers(2, client);
    }
    
    public static async Task<Guid> CreateConversation0Members(HttpClient client)
    {
        return await CreateConversationNMembers(0, client);
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

    private static async Task<Guid> CreateConversationNMembers(int n, HttpClient client)
    {
        List<Guid> members = [];
        for (int i = 0; i < n; i++)
        {
            members.Add(Guid.NewGuid());
        }
        
        var createConversationDto = new
        {
            TripId = Guid.NewGuid(),
            Title = "Test Conversation",
            Date = DateTime.UtcNow,
            Participants = members
        };
        var response = await client.PostAsJsonAsync("/api/v1/message/conversations", createConversationDto);
        var parsedResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var conversationId = parsedResponse?["conversationId"] != null
            ? Guid.Parse(parsedResponse["conversationId"].ToString() ?? "")
            : Guid.Empty;
        return conversationId == Guid.Empty ? throw new Exception("Failed to create conversation for test") : conversationId;
    }
}