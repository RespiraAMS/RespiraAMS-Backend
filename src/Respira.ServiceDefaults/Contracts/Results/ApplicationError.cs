namespace Respira.ServiceDefaults.Contracts.Results
{
    public sealed record ApplicationError
    {
        /// <summary>
        /// Operation status code
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// ApplicationError short description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// ApplicationError detail
        /// </summary>
        public object? Detail { get; set; }

        public ApplicationError(string statusCode, string description, object? detail = null)
        {
            // Check if status code is what our application supports
            if (!ApplicationStatus.IsSupportedStatusCode(statusCode))
            {
                throw new ArgumentException("Invalid status code", nameof(statusCode));
            }

            // Check if this status code is a failure code
            if (ApplicationStatus.IsSuccess(statusCode))
            {
                throw new ArgumentException(
                    "Invalid status code, error must be failure code",
                    nameof(statusCode)
                );
            }

            StatusCode = statusCode;
            Description = description;
            Detail = detail;
        }

        public override string ToString()
        {
            return @$"ApplicationError:
            ApplicationStatus: {StatusCode}
            Description: {Description}
            Detail: {Detail}";
        }
    }
}
