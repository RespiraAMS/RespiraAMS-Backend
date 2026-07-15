using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Middlewares;

namespace Respira.ServiceDefaults.Extensions;

/// <summary>
/// An extensions for registering and activate <see cref="GlobalExceptionHandler"/>
/// </summary>
public static class ErrorHandlingExtensions
{
    public static IServiceCollection AddCustomErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseCustomErrorHandling(this IApplicationBuilder app)
    {
        app.UseStatusCodePages(async context =>
        {
            var statusCode = context.HttpContext.Response.StatusCode;
            var message = statusCode switch
            {
                StatusCodes.Status401Unauthorized => "You do not have permission to access this resource.",
                StatusCodes.Status403Forbidden => "Access to this resource is forbidden.",
                StatusCodes.Status404NotFound => "The requested resource was not found.",
                _ => "An unexpected system error occurred. Please contact support."
            };

            context.HttpContext.Response.ContentType = "application/json";
            var errorResponse = ApiResponse<object>.Fail(message, statusCode);
            await context.HttpContext.Response.WriteAsJsonAsync(errorResponse);
        });

        app.UseExceptionHandler();
        return app;
    }
}