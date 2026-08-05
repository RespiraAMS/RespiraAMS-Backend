using Domain.Services.Contracts;
using Domain.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Domain;

public static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IDiagnoseService, CapDiagnoseService>();
    }
}