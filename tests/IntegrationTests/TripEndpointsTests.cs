using System.Net.Http.Json;
using Application.DTOs;
using Application.Interfaces;
using Core.Entities;
using Core.Enums;
using FluentAssertions;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

public class TripEndpointsTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public TripEndpointsTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthToken(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var jwtProvider = scope.ServiceProvider.GetRequiredService<IJwtProvider>();
        return jwtProvider.Generate(user);
    }

    [Fact]
    public async Task PostTrip_Authorized_ReturnsCreated()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            Name = "John",
            Surname = "Doe",
            Role = UserRole.REGULAR_USER,
            Sex = Sex.MALE
        };
        var token = await GetAuthToken(user);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dto = new CreateTripDTO
        {
            Route = new CreateRouteDTO
            {
                From = "Warsaw",
                To = "Krakow",
                BetweenPoints = new List<string>()
            },
            Price = 50,
            Date = DateTime.UtcNow.AddDays(1),
            MaxPassengers = 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/trips", dto);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<CreateTripResponseDTO>();
        content.Should().NotBeNull();
        content!.Price.Should().Be(50);
    }

    [Fact]
    public async Task GetTrips_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/trips");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<TripSummaryDTO>>();
        content.Should().NotBeNull();
    }
}
