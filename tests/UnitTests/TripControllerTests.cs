using System.Security.Claims;
using API.Controllers;
using Application.DTOs;
using Application.Interfaces.Trip;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace UnitTests;

public class TripControllerTests
{
    private readonly Mock<ITripService> _tripServiceMock;
    private readonly TripController _controller;

    public TripControllerTests()
    {
        _tripServiceMock = new Mock<ITripService>();
        _controller = new TripController(_tripServiceMock.Object);
    }

    private void SetUser(string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateTripDTO();
        var response = new CreateTripResponseDTO { TripId = Guid.NewGuid() };
        
        SetUser(userId.ToString());
        _tripServiceMock.Setup(s => s.CreateTrip(dto, userId)).ReturnsAsync(response);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.Location.Should().Be($"/api/trips/{response.TripId}");
        createdResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task Create_InvalidUserIdInToken_ReturnsUnauthorized()
    {
        // Arrange
        SetUser("invalid-guid");

        // Act
        var result = await _controller.Create(new CreateTripDTO());

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Search_ValidQuery_ReturnsOk()
    {
        // Arrange
        var results = new List<TripSummaryDTO>();
        _tripServiceMock.Setup(s => s.SearchTrips(It.IsAny<SearchTripsCriteria>())).ReturnsAsync(results);

        // Act
        var result = await _controller.Search("Warsaw", "Cracow", "2026-10-10");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(results);
        _tripServiceMock.Verify(s => s.SearchTrips(It.Is<SearchTripsCriteria>(c => 
            c.From == "Warsaw" && 
            c.To == "Cracow" && 
            c.Date.HasValue && 
            c.Date.Value.Year == 2026)), Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var response = new TripDetailsDTO { TripId = tripId };
        _tripServiceMock.Setup(s => s.GetById(tripId)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetById(tripId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }
}
