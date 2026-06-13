using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.DTOs;
using Users.Core;
using Users.Infrastructure;
using Xunit;

namespace IntegrationTests;

public class UsersApiTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private readonly HttpClient _client;

    public UsersApiTests(IntegrationTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Admin_CanBanAndUnbanUser()
    {
        // 1. Register two users: one admin, one regular
        var adminToken = await RegisterAndLogin("admin@test.com", "Admin123!", "Admin", "User", UserRole.ADMIN);
        var userToBan = await RegisterUser("toban@test.com", "User123!", "To", "Ban");

        // 2. Ban the user as admin
        var banDto = new BanUserDTO { Reason = "Bad behavior", Until = DateTime.UtcNow.AddDays(1) };
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        var banResponse = await _client.PostAsJsonAsync($"/api/users/{userToBan.Id}/ban", banDto);
        banResponse.EnsureSuccessStatusCode();

        // 3. Verify user is banned
        var userToBanToken = await Login("toban@test.com", "User123!");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToBanToken);
        
        var integrationDataResponse = await _client.GetAsync("/api/users/me/integration-data");
        integrationDataResponse.EnsureSuccessStatusCode();
        var integrationData = await integrationDataResponse.Content.ReadFromJsonAsync<UserIntegrationDTO>();
        integrationData!.Trip.CanCreateTrip.Should().BeFalse();

        // 4. Unban the user as admin
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        var unbanResponse = await _client.PostAsJsonAsync($"/api/users/{userToBan.Id}/unban", new { });
        unbanResponse.EnsureSuccessStatusCode();

        // 5. Verify user is unbanned
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToBanToken);
        integrationDataResponse = await _client.GetAsync("/api/users/me/integration-data");
        integrationDataResponse.EnsureSuccessStatusCode();
        integrationData = await integrationDataResponse.Content.ReadFromJsonAsync<UserIntegrationDTO>();
        integrationData!.Trip.CanCreateTrip.Should().BeTrue();
    }

    [Fact]
    public async Task User_CanRateAnotherUser()
    {
        // 1. Register two users
        var user1Token = await RegisterAndLogin("user1@test.com", "User123!", "User", "One");
        var user2 = await RegisterUser("user2@test.com", "User123!", "User", "Two");

        // 2. User 1 rates User 2
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Token);
        var rateResponse = await _client.PostAsJsonAsync($"/api/users/{user2.Id}/rating?comment=Good", 5);
        rateResponse.EnsureSuccessStatusCode();

        // 3. Verify rating
        var ratingsResponse = await _client.GetAsync($"/api/users/{user2.Id}/ratings");
        ratingsResponse.EnsureSuccessStatusCode();
        var ratings = await ratingsResponse.Content.ReadFromJsonAsync<List<RatingResponseDTO>>();
        ratings.Should().ContainSingle(r => r.Value == 5 && r.Comment == "Good");
    }

    private async Task<string> RegisterAndLogin(string email, string password, string name, string surname, UserRole? role = null)
    {
        var user = await RegisterUser(email, password, name, surname);
        
        if (role.HasValue && role.Value == UserRole.ADMIN)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var dbUser = await db.Users.FindAsync(user.Id);
            if (dbUser != null)
            {
                dbUser.Role = UserRole.ADMIN;
                await db.SaveChangesAsync();
            }
        }

        return await Login(email, password);
    }

    private async Task<AuthUserDTO> RegisterUser(string email, string password, string name, string surname)
    {
        var dto = new RegisterDTO 
        { 
            Email = email, 
            Password = password, 
            Name = name, 
            Surname = surname,
            DateOfBirth = new DateTime(1990, 1, 1),
            Sex = "MALE"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);
        response.EnsureSuccessStatusCode();

        var loginDto = new LoginDTO { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();
        return auth!.User;
    }

    private async Task<string> Login(string email, string password)
    {
        var loginDto = new LoginDTO { Email = email, Password = password };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();
        return auth!.Token;
    }
}
