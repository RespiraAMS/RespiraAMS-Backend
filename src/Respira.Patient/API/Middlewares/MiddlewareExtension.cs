namespace Respira.Patient.API.Middlewares
{
    public static class MiddlewareExtension
    {
        public static IApplicationBuilder UseDoctorMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<DoctorMiddleware>();
        }
    }
}
