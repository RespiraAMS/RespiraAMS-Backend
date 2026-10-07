using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.Clinical.Application;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.AntibioticGroups.CreateAntibioticGroup;
using Respira.Clinical.Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Respira.Clinical.Application.Features.Antibiotics.AddDosage;
using Respira.Clinical.Application.Features.Antibiotics.CreateAntibiotic;
using Respira.Clinical.Application.Features.Antibiotics.UpdateAntibiotic;
using Respira.Clinical.Application.Features.Antibiotics.UpdateDosage;
using Respira.Clinical.Application.Features.ClinicalVariables.CreateClinicalVariable;
using Respira.Clinical.Application.Features.ClinicalVariables.UpdateClinicalVariable;
using Respira.Clinical.Application.Features.Criteria.CreateCriterion;
using Respira.Clinical.Application.Features.Criteria.UpdateCriterion;
using Respira.Clinical.Application.Features.Pathogens.CreatePathogen;
using Respira.Clinical.Application.Features.Pathogens.UpdatePathogen;
using Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor;
using Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause;
using Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;

namespace Respira.Clinical.DI
{
    public static class DependencyInjection
    {
        public static void AddDI(this IHostApplicationBuilder builder)
        {
            AddDomain(builder);
            AddInfrastructure(builder);
            AddProfiles(builder.Services);
            AddFluentValidators(builder.Services);
        }

        # region Domain DI

        public static void AddDomain(this IHostApplicationBuilder builder)
        {
            builder.Services.AddScoped<IDiagnoseService, DiagnoseService>();
        }

        #endregion

        #region Application DI

        public static void AddProfiles(this IServiceCollection services)
        {
            services.AddScoped<ICreateMapper<CreateAntibioticGroupCommand, AntibioticGroup>, CreateAntibioticGroupMapper>();
            services.AddScoped<IUpdateMapper<AntibioticGroup, UpdateAntibioticGroupCommand>, UpdateAntibioticGroupMapper>();

            services.AddScoped<ICreateMapper<CreateAntibioticCommand, Antibiotic>, CreateAntibioticMapper>();
            services.AddScoped<ICreateMapper<AddDosageCommand, Dosage>, AddDosageMapper>();
            services.AddScoped<IUpdateMapper<Antibiotic, UpdateAntibioticCommand>, UpdateAntibioticMapper>();
            services.AddScoped<IUpdateMapper<Dosage, UpdateDosageCommand>, UpdateDosageMapper>();

            services.AddScoped<ICreateMapper<CreatePathogenCommand, Pathogen>, CreatePathogenMapper>();
            services.AddScoped<IUpdateMapper<Pathogen, UpdatePathogenCommand>, UpdatePathogenMapper>();

            services.AddScoped<ICreateMapper<CreateSuspectedCauseCommand, SuspectedCause>, CreateSuspectedCauseMapper>();
            services.AddScoped<IUpdateMapper<SuspectedCause, UpdateSuspectedCauseCommand>, UpdateSuspectedCauseMapper>();

            services.AddScoped<IMapper<Application.Features.Shared.ManageFormula.FormulaDto, Formula>, FormulaMapper>();

            services.AddScoped<ICreateMapper<CreateClinicalVariableCommand, ClinicalVariable>, CreateClinicalVariableMapper>();
            services.AddScoped<IUpdateMapper<ClinicalVariable, UpdateClinicalVariableCommand>, UpdateClinicalVariableMapper>();

            services.AddScoped<ICreateMapper<CreateCriterionCommand, Criterion>, CreateCriterionMapper>();
            services.AddScoped<IUpdateMapper<Criterion, UpdateCriterionCommand>, UpdateCriterionMapper>();

            services.AddScoped<ICreateMapper<CreateRiskFactorCommand, RiskFactor>, CreateRiskFactorMapper>();
            services.AddScoped<IUpdateMapper<RiskFactor, UpdateRiskFactorCommand>, UpdateRiskFactorMapper>();
        }

        public static void AddFluentValidators(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<ApplicationMarker>();
        }

        #endregion


        #region Infrastructure DI

        public static void AddInfrastructure(this IHostApplicationBuilder builder)
        {
            builder.AddNpgsqlDbContext<ClinicalDbContext>("clinicalDb");
            builder.Services.AddScoped<IDbContext, ClinicalDbContext>();
            builder.Services.Configure<SeedDataOptions>(builder.Configuration.GetSection(SeedDataOptions.SectionName));
            builder.Services.AddScoped<IPaginationFactory, PaginationFactory>();
        }

        public static void ApplyMigrations(this IHost host, bool isDevEnv)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ClinicalDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DbInitializer>>();
            try
            {
                context.Database.Migrate();
            }
            catch (Exception e)
            {
                if (isDevEnv)
                {
                    context.Database.EnsureDeleted();
                }

                logger.LogCritical("Failed to migrate database: {error}", e.Message);
            }
        }

        public static async Task SeedData(this WebApplication app)
        {
            // Only seed data in dev environment
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var provider = scope.ServiceProvider;
                var context = provider.GetRequiredService<ClinicalDbContext>();
                var options = provider.GetRequiredService<IOptions<SeedDataOptions>>();
                var logger = provider.GetRequiredService<ILogger<DbInitializer>>();
                await DbInitializer.InitializeAsync(context, options, logger);
            }
        }

        # endregion
    }
}
