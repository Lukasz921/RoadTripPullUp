namespace Application.TripPlanner;

public class SearchTripsV1RequestDTO
{
    public LatLngDTO Source { get; set; } = new();
    public LatLngDTO Target { get; set; } = new();
    public string DateFrom { get; set; } = string.Empty;
    public string DateTo { get; set; } = string.Empty;
    public decimal? MaxPrice { get; set; }
    public int MinSeats { get; set; } = 1;
    public string SortBy { get; set; } = "departure";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
