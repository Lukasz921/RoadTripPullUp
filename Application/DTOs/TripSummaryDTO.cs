namespace Application.DTOs;

public class TripSummaryDTO
{
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public int MaxPassengers { get; set; }
    public RouteDTO Route { get; set; } = new RouteDTO();
}

public class RouteDTO
{
    public Guid RouteId { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

