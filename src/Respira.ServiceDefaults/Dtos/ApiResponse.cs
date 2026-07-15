using Microsoft.AspNetCore.Http;

namespace Respira.ServiceDefaults.Dtos;

/// <summary>
/// API response
/// </summary>
/// <typeparam name="T">Response data type</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Response status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// True if request success
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// When request fail, message is guaranteed to exist.
    /// A success request may or may not have a message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Response data type.
    /// </summary>
    public T? Data { get; set; } 

    /// <summary>
    /// Create a success API response 
    /// </summary>
    /// <param name="data">The result data</param>
    /// <param name="message">Success message (if any)</param>
    /// <param name="statusCode">Success status code, default to 200</param>
    /// <returns>ApiResponse object</returns>
    public static ApiResponse<T> Ok(T data, string? message = null, int statusCode = StatusCodes.Status200OK)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode,
        };
    }
    
    /// <summary>
    /// Create a failure API response 
    /// </summary>
    /// <param name="message">Error message (required)</param>
    /// <param name="statusCode">Failure status code</param>
    /// <returns>ApiResponse object</returns>
    public static ApiResponse<T> Fail(string message, int statusCode = StatusCodes.Status500InternalServerError)
    {
        return new ApiResponse<T>
        {
            StatusCode = statusCode,
            Success = false,
            Message = message,
        };
    }
}

/// <summary>
/// API response
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Response status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// True if request success
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// When request fail, message is guaranteed to exist. A success request may or may not have a message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Create a success API response 
    /// </summary>
    /// <param name="message">Success message (if any)</param>
    /// <param name="statusCode">Success status code, default to 200</param>
    /// <returns>ApiResponse object</returns>
    public static ApiResponse Ok(string? message = null, int statusCode = StatusCodes.Status200OK)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            StatusCode = statusCode,
        };
    }

    /// <summary>
    /// Create a failure API response 
    /// </summary>
    /// <param name="message">Error message (required)</param>
    /// <param name="statusCode">Failure status code</param>
    /// <returns>ApiResponse object</returns>
    public static ApiResponse Fail(string? message = null, int statusCode = StatusCodes.Status500InternalServerError)
    {
        return new ApiResponse
        {
            StatusCode = statusCode,
            Success = false,
            Message = message,
        };
    }
}