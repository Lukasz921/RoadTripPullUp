namespace Application.Exceptions;

public class InvalidParametersException : Exception
{
    public InvalidParametersException() { }
    public InvalidParametersException(string message) : base(message) { }
}