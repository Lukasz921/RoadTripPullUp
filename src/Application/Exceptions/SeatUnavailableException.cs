namespace Application.Exceptions;

public class SeatUnavailableException : Exception
{
    public SeatUnavailableException() : base("No seats available for this trip.") { }
    public SeatUnavailableException(string message) : base(message) { }
}
