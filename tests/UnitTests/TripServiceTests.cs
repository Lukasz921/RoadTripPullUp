using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Trip;
using Application.Services;
using Core.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests;

public class TripServiceTests
{
    private readonly Mock<ITripRepository> _tripRepositoryMock;
    private readonly Mock<IRouteRepository> _routeRepositoryMock;
    private readonly TripService _tripService;

    public TripServiceTests()
    {
        _tripRepositoryMock = new Mock<ITripRepository>();
        _routeRepositoryMock = new Mock<IRouteRepository>();
        _tripService = new TripService(_tripRepositoryMock.Object, _routeRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateTrip_ValidInput_CreatesTripAndRoute_ReturnsResponse()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var dto = new CreateTripDTO
        {
            Route = new CreateRouteDTO
            {
                From = "Warsaw",
                To = "Cracow",
                BetweenPoints = new List<string> { " Radom " }
            },
            Price = 50.5f,
            Date = DateTime.UtcNow.AddDays(1),
            MaxPassengers = 3
        };

        // Act
        var result = await _tripService.CreateTrip(dto, driverId);

        // Assert
        result.Should().NotBeNull();
        result.Price.Should().Be(dto.Price);
        result.MaxPassengers.Should().Be(dto.MaxPassengers);
        
        _routeRepositoryMock.Verify(r => r.Save(It.Is<Route>(rt => 
            rt.From == "Warsaw" && 
            rt.To == "Cracow" && 
            rt.BetweenPoints.Contains("Radom"))), Times.Once);

        _tripRepositoryMock.Verify(t => t.Save(It.Is<Trip>(tr => 
            tr.DriverId == driverId && 
            tr.Price == dto.Price && 
            tr.MaxPassengers == dto.MaxPassengers &&
            tr.OfferStatus == TripStatus.Active)), Times.Once);
    }

    [Theory]
    [InlineData("", "Cracow", "Route origin and destination are required.")]
    [InlineData("Warsaw", "", "Route origin and destination are required.")]
    [InlineData("Warsaw", "Warsaw", "Route origin and destination cannot be the same.")]
    [InlineData("Warsaw", "Cracow", "Trip price must be greater than zero.", -1f)]
    [InlineData("Warsaw", "Cracow", "Max passengers must be greater than zero.", 50f, 0)]
    public async Task CreateTrip_InvalidInput_ThrowsValidationException(string from, string to, string expectedMessage, float price = 50f, int maxPassengers = 3)
    {
        // Arrange
        var dto = new CreateTripDTO
        {
            Route = new CreateRouteDTO { From = from, To = to },
            Price = price,
            Date = DateTime.UtcNow.AddDays(1),
            MaxPassengers = maxPassengers
        };

        // Act
        Func<Task> act = () => _tripService.CreateTrip(dto, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage(expectedMessage);
    }

    [Fact]
    public async Task CreateTrip_PastDate_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreateTripDTO
        {
            Route = new CreateRouteDTO { From = "Warsaw", To = "Cracow" },
            Price = 50f,
            Date = DateTime.UtcNow.AddDays(-1),
            MaxPassengers = 3
        };

        // Act
        Func<Task> act = () => _tripService.CreateTrip(dto, Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<ValidationException>().WithMessage("Trip date must be in the future.");
    }

    [Fact]
    public async Task SearchTrips_ValidCriteria_ReturnsTripsWithRoutes()
    {
        // Arrange
        var criteria = new SearchTripsCriteria { From = "Warsaw" };
        var routeId = Guid.NewGuid();
        var trips = new List<Trip>
        {
            new Trip { Id = Guid.NewGuid(), DriverId = Guid.NewGuid(), RouteId = routeId, Price = 50f, Date = DateTime.UtcNow.AddDays(1), MaxPassengers = 3, OfferStatus = TripStatus.Active }
        };
        var route = new Route { Id = routeId, From = "Warsaw", To = "Cracow" };

        _tripRepositoryMock.Setup(t => t.Search(It.IsAny<SearchTripsCriteria>())).ReturnsAsync(trips);
        _routeRepositoryMock.Setup(r => r.GetById(routeId)).ReturnsAsync(route);

        // Act
        var result = await _tripService.SearchTrips(criteria);

        // Assert
        result.Should().HaveCount(1);
        result[0].Route.From.Should().Be("Warsaw");
        _tripRepositoryMock.Verify(t => t.Search(It.Is<SearchTripsCriteria>(c => c.From == "Warsaw")), Times.Once);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsDetails()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var trip = new Trip { Id = tripId, DriverId = Guid.NewGuid(), RouteId = routeId, Price = 50f, Date = DateTime.UtcNow.AddDays(1), MaxPassengers = 3, OfferStatus = TripStatus.Active };
        var route = new Route { Id = routeId, From = "Warsaw", To = "Cracow" };

        _tripRepositoryMock.Setup(t => t.GetById(tripId)).ReturnsAsync(trip);
        _routeRepositoryMock.Setup(r => r.GetById(routeId)).ReturnsAsync(route);

        // Act
        var result = await _tripService.GetById(tripId);

        // Assert
        result.Should().NotBeNull();
        result.TripId.Should().Be(tripId);
        result.Route.From.Should().Be("Warsaw");
    }

    [Fact]
    public async Task GetById_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _tripRepositoryMock.Setup(t => t.GetById(tripId)).ReturnsAsync((Trip)null);

        // Act
        Func<Task> act = () => _tripService.GetById(tripId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
