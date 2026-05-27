using Core.TripPlanner;
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
        var passengerId = Guid.NewGuid();

        // Act
        var result = trip.TryAddPassenger(passengerId);

        // Assert
        result.Should().BeTrue();
        trip.PassengerIds.Should().Contain(passengerId);
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
        var passengerId = Guid.NewGuid();

        // Act
        var result = trip.TryAddPassenger(passengerId);

        // Assert
        result.Should().BeTrue();
        trip.PassengerIds.Should().HaveCount(1);
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
        var passengerId = Guid.NewGuid();

        // Act
        var result = trip.TryAddPassenger(passengerId);

        // Assert
        result.Should().BeFalse();
        trip.PassengerIds.Should().BeEmpty();
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
        trip.TryAddPassenger(Guid.NewGuid()); // Now status is Full

        var secondPassengerId = Guid.NewGuid();

        // Act
        var result = trip.TryAddPassenger(secondPassengerId);

        // Assert
        result.Should().BeFalse();
        trip.PassengerIds.Should().HaveCount(1);
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
        var driverAsPassengerId = driverId;

        // Act
        var result = trip.TryAddPassenger(driverAsPassengerId);

        // Assert
        result.Should().BeFalse();
        trip.PassengerIds.Should().BeEmpty();
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
        var passengerId = Guid.NewGuid();
        trip.TryAddPassenger(passengerId);

        // Act
        var result = trip.TryAddPassenger(passengerId);

        // Assert
        result.Should().BeFalse();
        trip.PassengerIds.Should().HaveCount(1);
    }
}
