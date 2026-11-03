namespace Respira.Patient.API.Middlewares
{
    public static class MiddlewareExtension
    {
        public static IApplicationBuilder UseAuthorizationMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuthMiddleware>();
        }
    }
}
