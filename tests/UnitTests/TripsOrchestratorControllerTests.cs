using System.Security.Claims;
using API.Controllers;
using API.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TripService.Application;
using Users.Application.DTOs;
using Users.Application.Interfaces;
using Xunit;
using MessageService.Application.Services;

namespace UnitTests;

public class TripsOrchestratorControllerTests
{
    private readonly Mock<ITripsService> _tripsServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<ILogger<TripsOrchestratorController>> _loggerMock;
    private readonly TripsOrchestratorController _controller;
    private readonly Guid _currentUserId = Guid.NewGuid();

    public TripsOrchestratorControllerTests()
    {
        _tripsServiceMock = new Mock<ITripsService>();
        _userServiceMock = new Mock<IUserService>();
        _conversationServiceMock = new Mock<IConversationService>();
        _loggerMock = new Mock<ILogger<TripsOrchestratorController>>();

        _controller = new TripsOrchestratorController(
            _tripsServiceMock.Object,
            _conversationServiceMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _currentUserId.ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task RateUser_ShouldSucceed_WhenPassengerRatesDriver()
    {
        // Arrange
        var tripId = "trip-123";
        var driverId = Guid.NewGuid();
        var trip = new TripDTO
        {
            Id = tripId,
            DriverId = driverId.ToString(),
            PassengerIds = new List<string> { _currentUserId.ToString() },
            DepartureTime = DateTime.UtcNow.AddHours(-1)
        };

        _tripsServiceMock.Setup(s => s.GetTripAsync(tripId)).ReturnsAsync(trip);

        var dto = new RateUserDTO { UserId = driverId, Value = 5, Comment = "Good driver" };

        // Act
        var result = await _controller.RateUser(tripId, dto);

        // Assert
        result.Should().BeOfType<OkResult>();
        _tripsServiceMock.Verify(s => s.RateTripAsync(tripId, _currentUserId.ToString(), It.IsAny<RateTripDTO>()), Times.Once);
        _userServiceMock.Verify(s => s.UpdateUserRating(driverId, 5), Times.Once);
    }

    [Fact]
    public async Task RateUser_ShouldReturnForbid_WhenCurrentUserDidNotParticipate()
    {
        // Arrange
        var tripId = "trip-123";
        var targetUserId = Guid.NewGuid();
        var trip = new TripDTO
        {
            Id = tripId,
            DriverId = Guid.NewGuid().ToString(),
            PassengerIds = new List<string>()
        };

        _tripsServiceMock.Setup(s => s.GetTripAsync(tripId)).ReturnsAsync(trip);

        var dto = new RateUserDTO { UserId = targetUserId, Value = 5 };

        // Act
        var result = await _controller.RateUser(tripId, dto);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RateUser_ShouldReturnBadRequest_WhenTargetUserDidNotParticipate()
    {
        // Arrange
        var tripId = "trip-123";
        var targetUserId = Guid.NewGuid();
        var trip = new TripDTO
        {
            Id = tripId,
            DriverId = _currentUserId.ToString(),
            PassengerIds = new List<string>()
        };

        _tripsServiceMock.Setup(s => s.GetTripAsync(tripId)).ReturnsAsync(trip);

        var dto = new RateUserDTO { UserId = targetUserId, Value = 5 };

        // Act
        var result = await _controller.RateUser(tripId, dto);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be("Target user didn't participate in this trip.");
    }

    [Fact]
    public async Task FileComplaint_ShouldSucceed_WhenParticipantComplainsAboutAnotherParticipant()
    {
        // Arrange
        var tripId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid();
        var trip = new TripDTO
        {
            Id = tripId,
            DriverId = Guid.NewGuid().ToString(),
            PassengerIds = new List<string> { _currentUserId.ToString(), targetUserId.ToString() },
            DepartureTime = DateTime.UtcNow.AddHours(1)
        };

        _tripsServiceMock.Setup(s => s.GetTripAsync(tripId)).ReturnsAsync(trip);

        var dto = new FileComplaintDTO { ComplainedUserId = targetUserId, Reason = "Rude" };

        // Act
        var result = await _controller.FileComplaint(tripId, dto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _userServiceMock.Verify(s => s.FileComplaint(_currentUserId, Guid.Parse(tripId), dto), Times.Once);
    }
}
