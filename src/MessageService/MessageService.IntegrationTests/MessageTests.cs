using System.Net;
using System.Net.Http.Json;

namespace MessageService.IntegrationTests;

[Collection("IntegrationTestCollection")]
public class MessageTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    
    public MessageTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateMessage_ReturnsCreated()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var createMessageDto = new
        {
            ConversationId = conversationId,
            Content = "Hello, world!",
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