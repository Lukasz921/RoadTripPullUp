using Application.DTOs;
using Core.Entities;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests;

public class TripRepositoryTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TripRepositoryTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Save_NewTrip_InsertsIntoDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = new TripRepository(context);
        var routeRepository = new RouteRepository(context);

        var driver = new User { Id = Guid.NewGuid(), Email = "driver@example.com", Name = "Driver", Surname = "User" };
        context.Users.Add(driver);
        await context.SaveChangesAsync();

        var route = new Route { Id = Guid.NewGuid(), From = "Warsaw", To = "Berlin" };
        await routeRepository.Save(route);

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverId = driver.Id,
            RouteId = route.Id,
            Price = 100,
            Date = DateTime.UtcNow.AddDays(1),
            MaxPassengers = 4,
            OfferStatus = TripStatus.Active
        };

        // Act
        await repository.Save(trip);

        // Assert
        var savedTrip = await repository.GetById(trip.Id);
        savedTrip.Should().NotBeNull();
        savedTrip!.Price.Should().Be(100);
        savedTrip.RouteId.Should().Be(route.Id);
    }

    [Fact]
    public async Task Search_FiltersByFromAndTo_ReturnsCorrectTrips()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = new TripRepository(context);
        var routeRepository = new RouteRepository(context);

        var driver = new User { Id = Guid.NewGuid(), Email = "driver2@example.com", Name = "Driver", Surname = "User" };
        context.Users.Add(driver);
        await context.SaveChangesAsync();

        var route1 = new Route { Id = Guid.NewGuid(), From = "Warsaw", To = "Berlin" };
        var route2 = new Route { Id = Guid.NewGuid(), From = "Krakow", To = "Prague" };
        await routeRepository.Save(route1);
        await routeRepository.Save(route2);

        var trip1 = new Trip { Id = Guid.NewGuid(), DriverId = driver.Id, RouteId = route1.Id, Price = 100, Date = DateTime.UtcNow.AddDays(1), MaxPassengers = 4, OfferStatus = TripStatus.Active };
        var trip2 = new Trip { Id = Guid.NewGuid(), DriverId = driver.Id, RouteId = route2.Id, Price = 150, Date = DateTime.UtcNow.AddDays(2), MaxPassengers = 2, OfferStatus = TripStatus.Active };
        await repository.Save(trip1);
        await repository.Save(trip2);

        // Act
        var criteria = new SearchTripsCriteria { From = "Warsaw", To = "Berlin" };
        var results = await repository.Search(criteria);

        // Assert
        results.Should().HaveCount(1);
        results[0].Id.Should().Be(trip1.Id);
    }
}
