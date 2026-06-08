namespace Application.Exceptions;

public class RoutingEngineUnavailableException : Exception
{
    public RoutingEngineUnavailableException() { }
    public RoutingEngineUnavailableException(string message) : base(message) { }
}
