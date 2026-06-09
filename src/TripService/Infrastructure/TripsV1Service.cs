using Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TripService.Application;

namespace TripService.Infrastructure;

public partial class TripsV1Service : ITripsV1Service
{
    private readonly string _connectionString;
    private readonly IRoutingEngine _routing;
    private readonly IJobStore _jobStore;
    private readonly IUserChecker _userChecker;

    public TripsV1Service(IConfiguration config, IRoutingEngine routing, IJobStore jobStore, IUserChecker userChecker)
    {
        _connectionString = config.GetConnectionString("TripConnection")
            ?? throw new InvalidOperationException("TripConnection is not configured.");
        _routing     = routing;
        _jobStore    = jobStore;
        _userChecker = userChecker;
    }
}
