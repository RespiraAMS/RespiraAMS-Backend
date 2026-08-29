using Application.Features.Patients.CreatePatient;
using Application.Features.Patients.DischargePatient;
using Application.Features.Treatments.CreateTreatment;
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
        services.AddScoped<ICreateMapper<Patient, CreatePatientCommand>, CreatePatientMapper>();
        services.AddScoped<IUpdateMapper<Patient, DischargePatientCommand>, DischargePatientMapper>();
        services.AddScoped<ICreateMapper<Treatment, CreateTreatmentCommand>, CreateTreatmentMapper>();
        services.AddScoped<IMapper<DiagnosisRecord, ValidateDiagnosisQuery>, ValidateDiagnosisMapper>();
    }

    public static void AddFluentValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ApplicationMarker>();
    }
}
