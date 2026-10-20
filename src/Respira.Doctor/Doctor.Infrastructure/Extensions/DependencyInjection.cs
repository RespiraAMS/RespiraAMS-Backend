using Microsoft.Extensions.DependencyInjection;
using Respira.Doctor.Application.Constracts.Data;
using Respira.Doctor.Infrastructure.Persistence;

namespace Respira.Doctor.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers infrastructure implementations that do not depend on a hosting provider.
        /// DbContext registration lives in the API composition root.
        /// </summary>
        public static IServiceCollection AddDoctorInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IDoctorDbContext>(sp => sp.GetRequiredService<DoctorDbContext>());

            return services;
        }
    }
}
