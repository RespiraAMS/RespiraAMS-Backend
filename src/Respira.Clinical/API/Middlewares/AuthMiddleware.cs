using System.Security.Claims;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.API.Middlewares
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
        private const string MedicalRegulationReviewerRole = "MedicalRegulationReviewer";
        private const string AdminRole = "Admin";

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

            // ClaimsPropagationMiddleware reconstructs the gateway identity for
            // service-specific authorization rules.
            var roles = context.User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (roles.Count == 0)
            {
                logger.LogDebug("No role claim set in request");
                await WriteResult(context, Result.Failure(new ApplicationError(ApplicationStatus.Unauthorized, "Unauthorized access")));
                return;
            }

            // Audit query and rollback are an explicit Admin-only exception to the
            // clinical role matrix. The gateway also applies an Admin policy to
            // the same route; this check protects direct service access.
            if (path.StartsWith("/api/v1/clinical/audit-logs", StringComparison.OrdinalIgnoreCase))
            {
                if (!roles.Contains(AdminRole))
                {
                    logger.LogDebug("Forbidden audit access: {Detail}", new { Roles = roles, path });
                    await WriteResult(
                        context,
                        Result.Failure(new ApplicationError(ApplicationStatus.Restricted, "Forbidden access")));
                    return;
                }

                await next(context);
                return;
            }

            /*
             * Authorization in clinical service:
             * 1. Doctor or HDD can use all the GET APIs and all diagnose APIs
             * 2. Medical regulation reviewer can use all the APIs except for
             * diagnose APIs
             * 3. Other role is restricted (including admin)
             */

            // Check for role if they are allow to access the endpoint
            if (roles.Contains(DoctorRole) || roles.Contains(HeadDepartmentDoctorRole))
            {
                var method = context.Request.Method;

                if (!method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                    (!method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                     !path.Contains("diagnose", StringComparison.OrdinalIgnoreCase)))
                {
                    logger.LogDebug("Forbidden access: {Detail}", new { Roles = roles, method, path });
                    await WriteResult(
                        context,
                        Result.Failure(new ApplicationError(ApplicationStatus.Restricted, "Forbidden access")));
                    return;
                }
            }
            else if (roles.Contains(MedicalRegulationReviewerRole))
            {
                if (path.Contains("diagnose", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug("Forbidden access: {Detail}", new { Roles = roles, context.Request.Method, path });
                    await WriteResult(
                        context,
                        Result.Failure(new ApplicationError(ApplicationStatus.Restricted, "Forbidden access")));
                    return;
                }
            }
            else
            {
                logger.LogDebug("Unauthorized role in request: {Roles}", roles);
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
