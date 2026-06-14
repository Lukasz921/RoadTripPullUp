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
}
