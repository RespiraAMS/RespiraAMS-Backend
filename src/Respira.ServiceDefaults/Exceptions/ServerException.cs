namespace Respira.ServiceDefaults.Exceptions;

/// <summary>
/// Internal server error exception. Use for when the error is from the server side
/// </summary>
public class ServerException() : Exception("Internal server error. Please try again")
{
}