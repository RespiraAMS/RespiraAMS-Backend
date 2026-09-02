namespace Respira.ServiceDefaults.Contracts.Results;

/*
 * Implementation of Result pattern (https://milanjovanovic.tech/blog/functional-error-handling-in-dotnet-with-the-result-pattern)
 */

/// <summary>
/// Result object with no data carried
/// </summary>
public sealed class Result
{
    /// <summary>
    /// Operation status code
    /// </summary>
    public string StatusCode { get; }

    /// <summary>
    /// Operation error if failed
    /// </summary>
    public Error? Error { get; }

    private Result(string statusCode, Error? error)
    {
        if (Status.IsSuccess(statusCode) && error != null)
        {
            throw new ArgumentException("Invalid result, a success result with non null error");
        }

        if (!Status.IsSuccess(statusCode) && error == null)
        {
            throw new ArgumentException("Invalid result, a failure result with null error");
        }

        if (!Status.IsSuccess(statusCode) && error != null && error.Code != statusCode)
        {
            throw new ArgumentException("Invalid result, a failure result with non matching error code");
        }

        StatusCode = statusCode;
        Error = error;
    }

    /// <summary>
    /// Construct a success result
    /// </summary>
    /// <param name="statusCode">Success status code</param>
    /// <returns>A success result</returns>
    public static Result Success(string statusCode) => new(statusCode, null);

    /// <summary>
    /// Construct a failure result
    /// </summary>
    /// <param name="error">Operation error</param>
    /// <returns>A failure result</returns
    public static Result Failure(Error error) => new(error.Code, error);

    /// <summary>
    /// Check if the result is a success result
    /// </summary>
    public bool IsSuccess => Status.IsSuccess(StatusCode);

    /// <summary>
    /// Check if the result is a failure result
    /// </summary>
    public bool IsFailure => !IsSuccess;
}

public sealed class Result<T>
{
    /// <summary>
    /// Operation status code
    /// </summary>
    public string StatusCode { get; }

    /// <summary>
    /// Operation error if failed
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Operation data result if success. When failure occurs, this proprty will be
    /// default value of T (so if it's not scalar value, it would be null). So when
    /// a failure is expected, this property should be checked before accessing it
    /// (or not accessed at all, since a failure wouldn't produce any result to pass)
    /// </summary>
    public T? Data { get; }

    private Result(string statusCode, Error? error, T? data)
    {
        if (Status.IsSuccess(statusCode) && error != null)
        {
            throw new ArgumentException("Invalid result, a success result with non null error");
        }

        if (!Status.IsSuccess(statusCode) && error == null)
        {
            throw new ArgumentException("Invalid result, a failure result with null error");
        }

        if (!Status.IsSuccess(statusCode) && error != null && error.Code != statusCode)
        {
            throw new ArgumentException("Invalid result, a failure result with non matching error code");
        }

        StatusCode = statusCode;
        Error = error;
        Data = data;
    }

    /// <summary>
    /// Construct a success result
    /// </summary>
    /// <param name="statusCode">Operation status code</param>
    /// <param name="data">Operation data result</param>
    /// <returns>A success result</returns>
    public static Result<T> Success(string statusCode, T? data) => new(statusCode, null, data);

    /// <summary>
    /// Construct a failure result
    /// </summary>
    /// <param name="error">Operation error</param>
    /// <returns>A failure result</returns
    public static Result<T> Failure(Error error) => new(error.Code, error, default);

    /// <summary>
    /// Check if the result is a success result
    /// </summary>
    public bool IsSuccess => Status.IsSuccess(StatusCode);

    /// <summary>
    /// Check if the result is a failure result
    /// </summary>
    public bool IsFailure => !IsSuccess;
}
