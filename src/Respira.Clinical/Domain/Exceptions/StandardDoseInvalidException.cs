using Domain.Enums;

namespace Domain.Exceptions
{
    /// <summary>
    /// Custom exception for DosageBusinessChecker: used when violating business rule with standard dose
    /// </summary>
    public class StandardDoseInvalidException : Exception
    {
        public StandardDoseInvalidException(RouteOfAdministration route) : base($"Standard dose for route {route} is not 1")
        {
        }

        public StandardDoseInvalidException(string? message) : base(message)
        {
        }

        public StandardDoseInvalidException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public StandardDoseInvalidException()
        {
        }
    }
}
