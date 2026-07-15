using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;

namespace Respira.ServiceDefaults.Middlewares;

/// <summary>
/// Global exception handler, used for all microservices for consistency error returned
/// </summary>
/// <param name="logger"></param>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log the error
        logger.LogError(
            exception,
            "Error occurred while handling request {RequestMethod} {RequestPath}: {ExceptionMessage}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            exception.Message);

        // Decide status code and error message based on exception type
        var (statusCode, message) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage))
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "You do not have permission to access this resource."
            ),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "The requested resource was not found."
            ),
            BadRequestException => (
                StatusCodes.Status400BadRequest,
                $"Bad request: {exception.Message}"
            ),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "The requested resource was not found."
            ),
            UnexpectedException => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred."
            ),
            ServerException => (
                StatusCodes.Status500InternalServerError,
                "An unexpected system error occurred. Please try again later."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected system error occurred. Please contact support."
            ),
        };

        // Create API response 
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        var errorResponse = ApiResponse<object>.Fail(message, statusCode);
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        // Write response
        await httpContext.Response.WriteAsJsonAsync(
            errorResponse,
            jsonOptions,
            cancellationToken
        );

        return true;
    }
}