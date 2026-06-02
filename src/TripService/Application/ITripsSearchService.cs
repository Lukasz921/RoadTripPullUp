namespace TripService.Application;

public interface ITripsSearchService
{
    Task<SyncSearchResultDTO> SearchAsync(SearchTripsQueryDTO query, CancellationToken ct = default);
}
