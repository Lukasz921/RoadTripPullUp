using Core.Entities;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public class TripDomainTests
{
    [Fact]
    public void TryAddPassenger_WhenActiveAndHasSpace_ReturnsTrueAndAddsPassenger()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverId = driverId,
            RouteId = Guid.NewGuid(),
            MaxPassengers = 2,
            OfferStatus = TripStatus.Active
        };
        var passenger = new User { Id = Guid.NewGuid() };

        // Act
        var result = trip.TryAddPassenger(passenger);

        // Assert
        result.Should().BeTrue();
        trip.Passengers.Should().Contain(passenger);
        trip.OfferStatus.Should().Be(TripStatus.Active);
    }

    [Fact]
    public void TryAddPassenger_WhenFull_ChangesStatusToFull()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverId = driverId,
            RouteId = Guid.NewGuid(),
            MaxPassengers = 1,
            OfferStatus = TripStatus.Active
        };
        var passenger = new User { Id = Guid.NewGuid() };

        // Act
        var result = trip.TryAddPassenger(passenger);

        // Assert
        result.Should().BeTrue();
        trip.Passengers.Should().HaveCount(1);
        trip.OfferStatus.Should().Be(TripStatus.Full);
    }

    [Fact]
    public void TryAddPassenger_WhenInactive_ReturnsFalse()
    {
        // Arrange
        var trip = new Trip
        {
            DriverId = Guid.NewGuid(),
            RouteId = Guid.NewGuid(),
            MaxPassengers = 5,
            OfferStatus = TripStatus.InActive
        };
        var passenger = new User { Id = Guid.NewGuid() };

        // Act
        var result = trip.TryAddPassenger(passenger);

        // Assert
        result.Should().BeFalse();
        trip.Passengers.Should().BeEmpty();
    }

    [Fact]
    public void TryAddPassenger_WhenAlreadyFull_ReturnsFalse()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverId = driverId,
            RouteId = Guid.NewGuid(),
            MaxPassengers = 1,
            OfferStatus = TripStatus.Active
        };
        trip.TryAddPassenger(new User { Id = Guid.NewGuid() }); // Now status is Full

        var secondPassenger = new User { Id = Guid.NewGuid() };

        // Act
        var result = trip.TryAddPassenger(secondPassenger);

        // Assert
        result.Should().BeFalse();
        trip.Passengers.Should().HaveCount(1);
    }

    [Fact]
    public void TryAddPassenger_WhenPassengerIsDriver_ReturnsFalse()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var trip = new Trip
        {
            DriverId = driverId,
            RouteId = Guid.NewGuid(),
            MaxPassengers = 5,
            OfferStatus = TripStatus.Active
        };
        var driverAsPassenger = new User { Id = driverId };

        // Act
        var result = trip.TryAddPassenger(driverAsPassenger);

        // Assert
        result.Should().BeFalse();
        trip.Passengers.Should().BeEmpty();
    }

    [Fact]
    public void TryAddPassenger_WhenPassengerAlreadyAdded_ReturnsFalse()
    {
        // Arrange
        var trip = new Trip
        {
            DriverId = Guid.NewGuid(),
            RouteId = Guid.NewGuid(),
            MaxPassengers = 5,
            OfferStatus = TripStatus.Active
        };
        var passenger = new User { Id = Guid.NewGuid() };
        trip.TryAddPassenger(passenger);

        // Act
        var result = trip.TryAddPassenger(passenger);

        // Assert
        result.Should().BeFalse();
        trip.Passengers.Should().HaveCount(1);
    }
}
