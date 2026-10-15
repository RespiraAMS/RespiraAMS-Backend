using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Respira.Patient.Application;
using Respira.Patient.Application.Contracts.Data;
using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Application.Features.Patients.AddNextOfKin;
using Respira.Patient.Application.Features.Patients.CreatePatient;
using Respira.Patient.Application.Features.Patients.UpdateNextOfKin;
using Respira.Patient.Domain.Entities;
using Respira.Patient.Infrastructure.Data;

namespace Respira.Patient.DI
{
    public static class DependencyInjection
    {
        public static void AddDI(this IHostApplicationBuilder builder)
        {
            AddInfrastructure(builder);
            AddProfiles(builder.Services);
            AddFluentValidators(builder.Services);
        }

        #region Application DI

        public static void AddProfiles(this IServiceCollection services)
        {
            services.AddScoped<ICreateMapper<CreatePatientCommand, Domain.Entities.Patient>, CreatePatientMapper>();
            services.AddScoped<ICreateMapper<AddNextOfKinCommand, NextOfKin>, AddNextOfKinMapper>();
            services.AddScoped<IUpdateMapper<NextOfKin, UpdateNextOfKinCommand>, UpdateNextOfKinMapper>();
        }

        public static void AddFluentValidators(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<ApplicationMarker>();
        }

        #endregion

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
