using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.API.Middlewares
{
    public class AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
    {
        private static async Task WriteResult(HttpContext context, Result result)
        {
            context.Response.StatusCode = ApplicationStatus.ToHttpStatusCode(result.StatusCode);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(result);
        }

        private const string DoctorRole = "Doctor";
        private const string HeadDepartmentDoctorRole = "HeadDepartmentDoctor";

        public async Task InvokeAsync(HttpContext context)
        {
            logger.LogDebug("Gateway headers stream down: {Headers}", new
            {
                Id = context.Request.Headers["X-ID"],
                Email = context.Request.Headers["X-Email"],
                Role = context.Request.Headers["X-Role"],
            });

            var path = context.Request.Path.Value;
            if (path is null)
            {
                logger.LogDebug("Request path is null");
                await WriteResult(
                    context,
                    Result.Failure(new ApplicationError(ApplicationStatus.ResourceNotFound, "Resouce not found")));
                return;
            }

            // Since Scalar only exists in dev environment, it doesn't need auth
            if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Path does not start with /api, skip auth: {path}", path);
                await next(context);
                return;
            }

            // Get the headers provided by the gateway
            var role = context.Request.Headers["X-Role"].FirstOrDefault();
            if (role is null)
            {
                logger.LogDebug("No role set in request header");
                await WriteResult(context, Result.Failure(new ApplicationError(ApplicationStatus.Unauthorized, "Unauthorized access")));
                return;
            }

            // Authorization in patient service: only doctor and HDD can use

            // Check for role if they are allow to access the endpoint
            if (!role.Equals(DoctorRole, StringComparison.OrdinalIgnoreCase) &&
                role.Equals(HeadDepartmentDoctorRole, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Unauthorized role in request header: {role}", role);
                await WriteResult(
                    context,
                    Result.Failure(new ApplicationError(ApplicationStatus.Restricted, "Forbidden access")));
                return;
            }

            // Forward to the next layer
            logger.LogDebug("Clinical auth middleware execute successfully, forward to next layer");
            await next(context);
        }
    }
}
