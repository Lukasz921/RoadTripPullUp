using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace MessageService.IntegrationTests;

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
    public async Task CreateConversation_MissingTripId_ReturnsBadRequest()
    {
        // Arrange
        var createConversationDto = new
        {
            Title = "Test Conversation",
            Date = DateTime.UtcNow,
            Participants = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/message/conversations", createConversationDto);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        // print responseData for debugging
        _testOutputHelper.WriteLine("Create Conversation Response Data:");
        var conversationId = responseData?["conversationId"] != null ? Guid.Parse(responseData["conversationId"] as string ?? "") : Guid.Empty;
        if (conversationId == Guid.Empty) throw new Exception("Failed to create conversation for test");
        
        // Act
        var response = await _client.GetAsync($"/api/v1/message/conversations/{conversationId}");
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversation = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(conversation);
        Assert.True(conversation.ContainsKey("id"));
        Assert.True(conversation.ContainsKey("tripId"));
    }
}