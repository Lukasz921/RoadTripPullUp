using Application.Exceptions;
using Microsoft.Extensions.Configuration;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsV1Service : ITripsV1Service
{
    private readonly ITripRepository _repository;
    private readonly IRoutingEngine  _routing;
    private readonly IJobStore       _jobStore;
    private readonly IUserChecker    _userChecker;

    public TripsV1Service(ITripRepository repository, IRoutingEngine routing, IJobStore jobStore, IUserChecker userChecker)
    {
        _repository  = repository;
        _routing     = routing;
        _jobStore    = jobStore;
        _userChecker = userChecker;
    }
}
