using JasperFx.Core;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.API.Middlewares;

public class DoctorMiddleware(RequestDelegate next, ILogger<DoctorMiddleware> logger)
{
    private static async Task WriteResult(HttpContext context, Result result)
    {
        context.Response.StatusCode = Status.ToHttpStatusCode(result.StatusCode);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(result);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogDebug("Gateway headers stream down: {headers}", new
        {
            Id = context.Request.Headers["X-ID"],
            Email = context.Request.Headers["X-Email"],
            Role = context.Request.Headers["X-Role"],
        });

        var path = context.Request.Path.Value;
        if (path is null)
        {
            logger.LogDebug("Request path is null");
            await WriteResult(context, Result.Failure(new Error(Status.ResourceNotFound, "Resouce not found")));
            return;
        }

        // Since Scalar only exists in dev environment, it doesn't need auth
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Path does not start with /api, skip auth: {path}", path);
            await next(context);
            return;
        }

        // Check if X-ID header is set and valid UUID
        var id = context.Request.Headers["X-ID"].FirstOrDefault();
        if (id is null)
        {
            logger.LogDebug("No ID set in request header");
            await WriteResult(context, Result.Failure(new Error(Status.Unauthorized, "Unauthorized access")));
            return;
        }
        if (!Guid.TryParse(id, out _))
        {
            logger.LogDebug("ID is not a valid UUID: {id}", id);
            await WriteResult(context, Result.Failure(new Error(Status.Unauthorized, "Unauthorized access")));
            return;
        }

        // Get the headers provided by the gateway
        var role = context.Request.Headers["X-Role"].FirstOrDefault();
        if (role is null)
        {
            logger.LogDebug("No role set in request header");
            await WriteResult(context, Result.Failure(new Error(Status.Unauthorized, "Unauthorized access")));
            return;
        }

        // Check for role if they are allow to access the endpoint
        // Patient service only allow doctor to access
        if (!role.EqualsIgnoreCase("doctor"))
        {
            logger.LogDebug("Role other than doctor access to patient service, auto reject: {role}", role);
            await WriteResult(context, Result.Failure(new Error(Status.Restricted, "Forbidden access")));
            return;
        }

        // Forward to the next layer
        logger.LogDebug("Patient doctor auth middleware execute successfully, forward to next layer");
        await next(context);
    }
}
