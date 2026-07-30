using Application.Features.AntibioticGroups.CreateAntibioticGroup;
using Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Application.Features.Antibiotics.AddDosage;
using Application.Features.Antibiotics.CreateAntibiotic;
using Application.Features.Antibiotics.UpdateAntibiotic;
using Application.Features.Antibiotics.UpdateDosage;
using Application.Features.Diseases.UpdateDisease;
using Application.Features.Pathogens.CreatePathogen;
using Application.Features.Pathogens.UpdatePathogen;
using Application.Features.Shared.ManageCriterion;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

/// <summary>
///  This is just a dump class used for scanning assembly, it has no meaning in application code
/// </summary>
public class ApplicationMarker;

public static class DependencyInjection
{
    public static void AddProfiles(this IServiceCollection services)
    {
        services.AddScoped<ICreateMapper<AntibioticGroup, CreateAntibioticGroupCommand>, CreateAntibioticGroupMapper>();
        services.AddScoped<IUpdateMapper<AntibioticGroup, UpdateAntibioticGroupCommand>, UpdateAntibioticGroupMapper>();

        services.AddScoped<ICreateMapper<Pathogen, CreatePathogenCommand>, CreatePathogenMapper>();
        services.AddScoped<IUpdateMapper<Pathogen, UpdatePathogenCommand>, UpdatePathogenMapper>();

        services.AddScoped<ICreateMapper<Antibiotic, CreateAntibioticCommand>, CreateAntibioticMapper>();
        services.AddScoped<ICreateMapper<Dosage, AddDosageCommand>, AddDosageMapper>();
        services.AddScoped<IUpdateMapper<Antibiotic, UpdateAntibioticCommand>, UpdateAntibioticMapper>();
        services.AddScoped<IUpdateMapper<Dosage, UpdateDosageCommand>, UpdateDosageMapper>();

        services.AddScoped<ICreateMapper<Criterion, CreateCriterionCommand>, CreateCriterionMapper>();
        services.AddScoped<IUpdateMapper<Criterion, UpdateCriterionCommand>, UpdateCriterionMapper>();
        services.AddScoped<IResultMapper<Criterion, CriterionItem>, CriterionResultMapper>();

        services.AddScoped<IUpdateMapper<Disease, UpdateDiseaseCommand>, UpdateDiseaseMapper>();
    }

    public static void AddFluentValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ApplicationMarker>();
    }
}