namespace Respira.ServiceDefaults.Exceptions;

/// <summary>
/// Internal server error exception. Use for when the error is from the server side
/// </summary>
public class ServerException : Exception
{
    public ServerException() : base("Internal server error. Please try again")
    {
    }

    public ServerException(Exception innerException) : base("Internal server error. Please try again", innerException)
    {
    }
}