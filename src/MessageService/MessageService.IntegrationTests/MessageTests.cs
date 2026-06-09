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
}