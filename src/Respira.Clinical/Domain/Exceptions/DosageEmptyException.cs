namespace Domain.Exceptions
{
    /// <summary>
    /// Custom exception for DosageBusinessChecker: used when dosage is empty
    /// </summary>
    public class DosageEmptyException : Exception
    {
        public DosageEmptyException() : base("Dosage is empty")
        {
        }

        public DosageEmptyException(string? message) : base(message)
        {
        }

        public DosageEmptyException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
