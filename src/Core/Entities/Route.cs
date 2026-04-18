using Core.Enums;

namespace Core.Entities;

public class Route
{
    public Guid Id { get; set; }
    public required string From {get; set;}
    public required string To {get; set;}
    public  List<string> BetweenPoints {get; set;} = new ();

}