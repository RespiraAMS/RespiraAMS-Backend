using Domain.Enums;
using Range = Domain.Models.Range;

namespace Domain.Exceptions
{
    /// <summary>
    /// Custom exception for DosageBusinessChecker: used when dosage violate rule "do not have overlapped CrCl range"
    /// </summary>
    public class OverlappedCrclException : Exception
    {
        public OverlappedCrclException()
        {
        }

        public OverlappedCrclException(RouteOfAdministration route, Range crcl1, Range crcl2) : base($"Route {route} has dosage CrCl overlapped: {crcl1} - {crcl2}")
        {
        }
        public OverlappedCrclException(string? message) : base(message)
        {
        }

        public OverlappedCrclException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
