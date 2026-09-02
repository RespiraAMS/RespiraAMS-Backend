namespace Respira.ServiceDefaults.Contracts.Results;

public sealed record Error
{
    /// <summary>
    /// Operation status code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Error short description
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Error detail
    /// </summary>
    public object? Detail { get; set; }

    public Error(string code, string description, object? detail)
    {
        // Check if status code is what our application supports
        if (!Status.IsSupportedStatusCode(code))
        {
            throw new ArgumentException("Invalid status code", nameof(code));
        }

        // Check if this status code is a failure code
        if (Status.IsSuccess(code))
        {
            throw new ArgumentException("Invalid status code, error must be failure code", nameof(code));
        }

        Code = code;
        Description = description;
        Detail = detail;
    }

    public Error(string code, string description) : this(code, description, null) { }

    public override string ToString()
    {
        return @$"Error:
            Status: {Code}
            Description: {Description}
            Detail: {Detail}";
    }
}

