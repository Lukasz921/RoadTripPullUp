namespace Application.TripPlanner;

public class CreateTripDTO
{
    public CreateRouteDTO Route { get; set; } = new();
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public int MaxPassengers { get; set; }
}

public class CreateRouteDTO
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public List<string> BetweenPoints { get; set; } = new();
}
