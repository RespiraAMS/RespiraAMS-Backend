namespace Respira.ServiceDefaults.Exceptions;

/// <summary>
/// Exception when something unexpectedly happen (for example, a method that can only return a value in range
/// [1, 5] suddenly return 10). This exception would be useful when testing logic
/// </summary>
/// <param name="message">Exception message</param>
public class UnexpectedException(string message) : Exception(message)
{
}