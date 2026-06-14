using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Abstractions;

namespace MessageService.IntegrationTests;

[Collection("IntegrationTestCollection")]
[Trait("Category", "E2E")]
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
        var (conversationId, _) = await IntegrationTestCommon.CreateConversation2Members(_client);
        
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

    [Fact]
    public async Task GetNonExistingConversation_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/message/conversations/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateConversationWithoutParticipants_ReturnsBadRequest()
    {
        // Arrange
        var createConversationDto = new
        {
            TripId = Guid.NewGuid(),
            Title = "Test Conversation",
            Date = DateTime.UtcNow,
            Participants = new List<Guid>() // empty participants
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/message/conversations", createConversationDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task GetConversationForTrip_ReturnsOk()
    {
        // Arrange
        var (_, tripId) = await IntegrationTestCommon.CreateConversation2Members(_client);
        
        // Act
        var response = await _client.GetAsync($"/api/v1/message/conversations/byTripId/direct/{tripId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversation = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.NotNull(conversation);
        Assert.Single(conversation); // only one conversation created for the trip in this test
        var conv = conversation[0];
        Assert.True(conv.ContainsKey("conversationId"));
        Assert.True(conv.ContainsKey("tripId"));
    }

    [Fact]
    public async Task GetNonExistingConversationForTrip_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/message/conversations/byTripId/group/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDirectConversationsForTrip_ReturnsOk()
    {
        // Arrange
        var (_, tripId) = await IntegrationTestCommon.CreateConversation2Members(_client);

        // Act
        var response = await _client.GetAsync($"/api/v1/message/conversations/byTripId/direct/{tripId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversations = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.NotNull(conversations);
        Assert.Single(conversations); // only one conversation created for the trip in this test
        var conversation = conversations[0];
        Assert.True(conversation.ContainsKey("conversationId"));
        Assert.True(conversation.ContainsKey("tripId"));
    }

    [Fact]
    public async Task GetDirectConversationsForTripWithNoConversations_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/message/conversations/byTripId/direct/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversations = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.NotNull(conversations);
        Assert.Empty(conversations); // no conversations for the trip
    }
}