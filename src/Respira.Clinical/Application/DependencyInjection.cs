using Application.Features.AntibioticGroups.CreateAntibioticGroup;
using Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Application.Features.Pathogens.CreatePathogen;
using Application.Features.Pathogens.UpdatePathogen;
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
    }

    public static void AddFluentValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ApplicationMarker>();
    }
}