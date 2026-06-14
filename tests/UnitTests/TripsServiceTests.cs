using Moq;
using TripService.Application;
using TripService.Infrastructure;
using TripService.Infrastructure.Repositories;
using MessageService.Core.Exceptions;
using Xunit;
using FluentAssertions;

namespace UnitTests;

public class TripsServiceTests
{
    private readonly Mock<ITripRepository> _repositoryMock = new();
    private readonly Mock<IRoutingEngine> _routingMock = new();
    private readonly Mock<IJobStore> _jobStoreMock = new();
    private readonly Mock<IUserChecker> _userCheckerMock = new();
    private readonly TripsService _service;

    public TripsServiceTests()
    {
        _service = new TripsService(
            _repositoryMock.Object,
            _routingMock.Object,
            _jobStoreMock.Object,
            _userCheckerMock.Object);
    }

    [Fact]
    public async Task CreateTripAsync_ShouldThrowForbiddenException_WhenUserIsBanned()
    {
        // Arrange
        var driverId = Guid.NewGuid().ToString();
        _userCheckerMock.Setup(x => x.IsUserBannedAsync(driverId, default)).ReturnsAsync(true);

        var dto = new CreateTripDTO
        {
            DepartureTime = DateTime.UtcNow.AddDays(1),
            MaxDetourMeters = 1000,
            PricePerSeat = 10,
            AvailableSeats = 3
        };

        // Act & Assert
        await _service.Awaiting(s => s.CreateTripAsync(dto, driverId))
            .Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Banned users cannot create trips.");
    }

    [Fact]
    public async Task AddPassengerAsync_ShouldThrowForbiddenException_WhenPassengerIsBanned()
    {
        // Arrange
        var tripId = Guid.NewGuid().ToString();
        var driverId = Guid.NewGuid().ToString();
        var passengerId = Guid.NewGuid().ToString();

        _userCheckerMock.Setup(x => x.UserExistsAsync(passengerId, default)).ReturnsAsync(true);
        _userCheckerMock.Setup(x => x.IsUserBannedAsync(passengerId, default)).ReturnsAsync(true);

        // Act & Assert
        await _service.Awaiting(s => s.AddPassengerAsync(tripId, driverId, passengerId))
            .Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Banned users cannot join trips.");
    }

    [Fact]
    public async Task RateTripAsync_ShouldThrowForbiddenException_WhenRaterIsBanned()
    {
        // Arrange
        var tripId = Guid.NewGuid().ToString();
        var raterId = Guid.NewGuid().ToString();
        var dto = new RateTripDTO { Rating = 5 };

        _userCheckerMock.Setup(x => x.IsUserBannedAsync(raterId, default)).ReturnsAsync(true);

        // Act & Assert
        await _service.Awaiting(s => s.RateTripAsync(tripId, raterId, dto))
            .Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Banned users cannot rate trips.");
    }
}
