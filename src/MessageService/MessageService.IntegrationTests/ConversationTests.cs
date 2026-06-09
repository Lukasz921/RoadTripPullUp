using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace MessageService.IntegrationTests;

[Collection("IntegrationTestCollection")]
public class ConversationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly HttpClient _client;
    
    public ConversationTests(CustomWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task CreateConversation_ReturnsCreated()
    {
        // Arrange
        var createConversationDto = new
        {
            TripId = Guid.NewGuid(),
            Title = "Test Conversation",
            Date = DateTime.UtcNow,
            Participants = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/message/conversations", createConversationDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        Assert.NotNull(responseData);
        Assert.True(responseData.ContainsKey("conversationId"));
    }
    
    
    [Fact]
    public async Task GetConversation_ReturnsOk()
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
        var responseData = await firstResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        var conversationId = responseData?["conversationId"] != null ? Guid.Parse(responseData["conversationId"].ToString() ?? "") : Guid.Empty;
        if (conversationId == Guid.Empty) throw new Exception("Failed to create conversation for test");
        
        // Act
        var response = await _client.GetAsync($"/api/v1/message/conversations/{conversationId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversation = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(conversation);
        Assert.True(conversation.ContainsKey("conversationId"));
        Assert.True(conversation.ContainsKey("tripId"));
        var participants = conversation["participants"] as JsonElement?;
        Assert.NotNull(participants);
        Assert.Equal(JsonValueKind.Array, participants.Value.ValueKind);
        Assert.Equal(3, participants.Value.GetArrayLength()); // creator + 2 participants
    }
}