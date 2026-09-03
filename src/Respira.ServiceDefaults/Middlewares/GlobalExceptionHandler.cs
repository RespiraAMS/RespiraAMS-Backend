using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;

namespace Respira.ServiceDefaults.Middlewares;

/// <summary>
/// Global exception handler, used for all microservices for consistency error returned
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unhandled exception: {Message}", exception.Message);

        // Since Wolverine built in Fluent validation middleware throw exception instead of our Result,
        // we'll handle that exception separately to other exceptions, to follow the Result pattern
        // correctly
        if (exception is ValidationException valEx)
        {
            logger.LogError("Validation exception: {Message}", valEx.Message);

            // Create result object
            var msg = string.Join("; ", valEx.Errors.Select(e => e.ErrorMessage));
            var result = Result.Failure(new Error(Status.BadRequest, msg));

            httpContext.Response.StatusCode = Status.ToHttpStatusCode(result.StatusCode);
            httpContext.Response.ContentType = "application/json";

            // Write response
            await httpContext.Response.WriteAsJsonAsync(result, _jsonOptions, cancellationToken);
            return true;
        }

        // Handle other exception normally
        var path = $"{httpContext.Request.Method} {httpContext.Request.Path}";
        logger.LogError("Unexpected error occured: {Detail}", new
        {
            Path = path,
            exception.Message,
            exception.StackTrace,
        });

        // Create API response 
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";
        var res = Result.Failure(new Error(Status.ServerError, exception.Message));

        // Write response
        await httpContext.Response.WriteAsJsonAsync(res, _jsonOptions, cancellationToken);
        return true;
    }
}
