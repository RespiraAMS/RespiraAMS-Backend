namespace Respira.ServiceDefaults.Exceptions;

/// <summary>
/// Bad request exception. Use for client side error
/// </summary>
/// <param name="message">Exception message</param>
public class BadRequestException(string message) : Exception(message)
{
    
}