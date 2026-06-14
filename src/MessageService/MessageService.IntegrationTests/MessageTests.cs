using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit.Abstractions;

namespace MessageService.IntegrationTests;

[Collection("IntegrationTestCollection")]
[Trait("Category", "E2E")]
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
        var (conversationId, _) = await IntegrationTestCommon.CreateConversation2Members(_client);
        
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
        var (conversationId, _) = await IntegrationTestCommon.CreateConversation2Members(_client);
        var messageId = await IntegrationTestCommon.CreateMessage(conversationId, _client);
        
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
        var (conversationId, _) = await IntegrationTestCommon.CreateConversation2Members(_client);
        
        for (int i = 0; i < 5; i++)
        {
            await IntegrationTestCommon.CreateMessage(conversationId, _client);
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