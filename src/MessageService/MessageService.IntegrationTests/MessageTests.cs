using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit.Abstractions;

namespace MessageService.IntegrationTests;

[Collection("IntegrationTestCollection")]
public class MessageTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    
    public MessageTests(CustomWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateMessage_ReturnsCreated()
    {
        // Arrange
        var createConversationDto = new
        {
            TripId = Guid.NewGuid(),
            Title = "Test Conversation",
            Date = DateTime.UtcNow,
            Participants = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/message/conversations", createConversationDto);
        var response1 = await firstResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var conversationId = response1?["conversationId"] != null ? Guid.Parse(response1["conversationId"].ToString() ?? "") : Guid.Empty;
        if (conversationId == Guid.Empty) throw new Exception("Failed to create conversation for test");
        
        
        var createMessageDto = new
        {
            ConversationId = conversationId,
            Payload = new JsonObject { ["text"] = "Hello, world!" },
            Type = "text"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/message/messages", createMessageDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        Assert.NotNull(responseData);
        Assert.True(responseData.ContainsKey("messageId"));
    }

    [Fact]
    public async Task GetMessage_ReturnsOk()
    {
        // Arrange
        var createConversationDto = new
        {
            TripId = Guid.NewGuid(),
            Title = "Test Conversation",
            Date = DateTime.UtcNow,
            Participants = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/message/conversations", createConversationDto);
        var response1 = await firstResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var conversationId = response1?["conversationId"] != null
            ? Guid.Parse(response1["conversationId"].ToString() ?? "")
            : Guid.Empty;
        if (conversationId == Guid.Empty) throw new Exception("Failed to create conversation for test");
        
        var createMessageDto = new
        {
            ConversationId = conversationId,
            Payload = new JsonObject { ["text"] = "Hello, world!" },
            Type = "text"
        };
        
        var messageResponse = await _client.PostAsJsonAsync("/api/v1/message/messages", createMessageDto);
        var response2 = await messageResponse.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        var messageId = response2?["messageId"] != null ? Guid.Parse(response2["messageId"].ToString()) : Guid.Empty;
        if (messageId == Guid.Empty) throw new Exception("Failed to create message for test");
        
        // Act
        var response = await _client.GetAsync($"/api/v1/message/messages/{messageId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        
        Assert.NotNull(message);
        Assert.True(message.ContainsKey("messageId"));
        // parse payload as JsonObject
        var payload = JsonNode.Parse(message["payload"].ToString() ?? "null");
        Assert.NotNull(payload);
        Assert.True(payload.AsObject().ContainsKey("text"));
        Assert.Equal("Hello, world!", payload["text"]?.ToString());
    }
    
    [Fact]
    public async Task GetMessagesForConversation_ReturnsOk()
    {
        // Arrange
        var createConversationDto = new
        {
            TripId = Guid.NewGuid(),
            Title = "Test Conversation",
            Date = DateTime.UtcNow,
            Participants = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/message/conversations", createConversationDto);
        var response1 = await firstResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var conversationId = response1?["conversationId"] != null
            ? Guid.Parse(response1["conversationId"].ToString() ?? "")
            : Guid.Empty;
        if (conversationId == Guid.Empty) throw new Exception("Failed to create conversation for test");
        
        for (int i = 0; i < 5; i++)
        {
            var createMessageDto = new
            {
                ConversationId = conversationId,
                Payload = new JsonObject { ["text"] = $"Message {i}" },
                Type = "text"
            };
            await _client.PostAsJsonAsync("/api/v1/message/messages", createMessageDto);
        }
        
        // Act
        var response = await _client.GetAsync($"/api/v1/message/conversations/{conversationId}/messages");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.NotNull(messages);
        Assert.Equal(5, messages.Count);
    }
}