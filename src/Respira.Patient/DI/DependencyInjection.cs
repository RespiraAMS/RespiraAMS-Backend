using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Infrastructure.Data;

namespace Respira.Patient.DI
{
    public static class DependencyInjection
    {
        public static void AddDI(this IHostApplicationBuilder builder)
        {
            AddInfrastructure(builder);
        }

        #region Infrastructure DI

        public static void AddInfrastructure(this IHostApplicationBuilder builder)
        {
            builder.AddNpgsqlDbContext<PatientDbContext>("patientDb");
            builder.Services.AddScoped<IDbContext, PatientDbContext>();
        }

        public static void ApplyMigrations(this IHost host, bool isDevEnv)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PatientDbContext>();
            try
            {
                context.Database.Migrate();
            }
            catch (Exception)
            {
                if (isDevEnv)
                {
                    context.Database.EnsureDeleted();
                }
            }
        }

        #endregion
    }
}
