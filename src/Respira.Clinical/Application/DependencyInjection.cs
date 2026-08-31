using Application.Features.Antibiograms.CreateAntibiogram;
using Application.Features.Antibiograms.UpdateAntibiogram;
using Application.Features.AntibioticGroups.CreateAntibioticGroup;
using Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Application.Features.Antibiotics.AddDosage;
using Application.Features.Antibiotics.CreateAntibiotic;
using Application.Features.Antibiotics.UpdateAntibiotic;
using Application.Features.Antibiotics.UpdateDosage;
using Application.Features.Causes.CreateCause;
using Application.Features.Causes.UpdateCause;
using Application.Features.Diagnose.EmpiricalDiagnose;
using Application.Features.Diagnose.TargetedDiagnose;
using Application.Features.Diseases.UpdateDisease;
using Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;
using Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;
using Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;
using Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;
using Application.Features.Pathogens.CreatePathogen;
using Application.Features.Pathogens.UpdatePathogen;
using Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;
using Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;
using Application.Features.Shared.ManageCriterion;
using Domain.Services.Dtos;
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

        services.AddScoped<ICreateMapper<Cause, CreateCauseCommand>, CreateCauseMapper>();
        services.AddScoped<IUpdateMapper<Cause, UpdateCauseCommand>, UpdateCauseMapper>();

        services.AddScoped<
            ICreateMapper<IcuHospitalizeCriterion, CreateIcuHospitalizeCriterionCommand>,
            CreateIcuHospitalizeCriterionMapper>();
        services.AddScoped<
            IUpdateMapper<IcuHospitalizeCriterion, UpdateIcuHospitalizeCriterionCommand>,
            UpdateIcuHospitalizeCriterionMapper>();

        services.AddScoped<
            ICreateMapper<ResistanceRiskFactor, CreateResistanceRiskFactorCommand>,
            CreateResistanceRiskFactorMapper>();
        services.AddScoped<
            IUpdateMapper<ResistanceRiskFactor, UpdateResistanceRiskFactorCommand>,
            UpdateResistanceRiskFactorMapper>();

        services.AddScoped<
            ICreateMapper<EmpiricTreatmentProtocol, CreateEmpiricTreatmentProtocolCommand>,
            CreateEmpiricTreatmentProtocolMapper>();
        services.AddScoped<
            IUpdateMapper<EmpiricTreatmentProtocol, UpdateEmpiricTreatmentProtocolCommand>,
            UpdateEmpiricTreatmentProtocolMapper>();

        services.AddScoped<ICreateMapper<Antibiogram, CreateAntibiogramCommand>, CreateAntibiogramMapper>();
        services.AddScoped<IUpdateMapper<Antibiogram, UpdateAntibiogramCommand>, UpdateAntibiogramMapper>();

        services.AddScoped<IMapper<TargetedDiagnoseQuery, PatientInfo>, TargetedDiagnoseMapper>();
        services.AddScoped<IMapper<EmpiricalDiagnoseQuery, PatientInfo>, EmpiricalDiagnosePatientInfoMapper>();
        services.AddScoped<IMapper<EmpiricalDiagnoseQuery, ClinicalPicture>, EmpiricalDiagnoseClinicalPictureMapper>();
    }

    public static void AddFluentValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ApplicationMarker>();
    }
}
