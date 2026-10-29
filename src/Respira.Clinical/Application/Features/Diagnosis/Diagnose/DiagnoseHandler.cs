using System.Globalization;
using ImTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Diagnosis.Diagnose
{
    public class DiagnoseHandler(IDbContext context, IDiagnoseService service, ILogger<DiagnoseHandler> logger)
        : IQueryHandler<DiagnoseQuery, Result<DiagnoseResult>>
    {
        public async Task<Result<DiagnoseResult>> HandleAsync(DiagnoseQuery query, CancellationToken cancellationToken = default)
        {
            // Get the clinical context for diagnosis
            var clinicalContext = new ClinicalContext
            {
                Variables = await context.ClinicalVariables
                    .AsNoTracking()
                    .ToListAsync(cancellationToken),
                Pathogens = await context.Pathogens
                    .AsNoTracking()
                    .Include(x => x.RiskFactors)
                    .ThenInclude(f => f.Criterion)
                    .ToListAsync(cancellationToken),
                SuspectedCauses = await context.SuspectedCauses
                    .AsNoTracking()
                    .Include(x => x.Pathogen)
                    .ThenInclude(p => p.RiskFactors)
                    .ThenInclude(f => f.Criterion)
                    .ToListAsync(cancellationToken),
                Metrics = await context.ClinicalMetrics
                    .AsNoTracking()
                    .Include(x => x.Rules)
                    .ThenInclude(r => r.Criterion)
                    .Where(x => x.IsApplied)
                    .ToListAsync(cancellationToken),
                Antibiotics = await context.Antibiotics
                    .AsNoTracking()
                    .Include(x => x.AntibioticGroup)
                    .ToListAsync(cancellationToken),
                AntibioticGroups = await context.AntibioticGroups
                    .AsNoTracking()
                    .Include(x => x.Parent)
                    .ToListAsync(cancellationToken),
                Antibiograms = await context.Antibiograms
                    .AsNoTracking()
                    .Include(x => x.Pathogen)
                    .Include(x => x.Antibiotics)
                    .ToListAsync(cancellationToken),
                TreatmentProtocols = await context.Treatments
                    .AsNoTracking()
                    .Include(x => x.Pathogens)
                    .Include(x => x.Criteria)
                    .Include(x => x.Medicines)
                    .ThenInclude(x => x.Antibiotics)
                    .ThenInclude(x => x.AntibioticGroup)
                    .ToListAsync(cancellationToken),
            };

            // Validate the observations: check if ID exists and value match with variable
            var observations = new List<ClinicalObservation>();
            foreach (var observation in query.Observations)
            {
                if (clinicalContext.Variables.All(x => x.Id != observation.VariableId))
                {
                    logger.LogDebug("Variable {variableId} not found in clinical context", observation.VariableId);
                    return Result<DiagnoseResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Variable not found"));
                }

                var variable = clinicalContext.Variables.First(x => x.Id == observation.VariableId);
                switch (variable.ValueType)
                {
                    case ClinicalValueType.Boolean:
                        if (!bool.TryParse(observation.Value, out var value))
                        {
                            logger.LogDebug("Receive boolean variable, but value observed is not boolean: {observation}", observation);
                            return Result<DiagnoseResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Invalid observation value"));
                        }

                        observations.Add(new ClinicalObservation(variable, value));
                        break;
                    case ClinicalValueType.Numeric:
                        if (!decimal.TryParse(observation.Value, CultureInfo.InvariantCulture, out var numericValue))
                        {
                            logger.LogDebug("Receive numeric variable, but value observed is not numeric: {observation}", observation);
                            return Result<DiagnoseResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Invalid observation value"));
                        }

                        observations.Add(new ClinicalObservation(variable, numericValue));
                        break;
                    case ClinicalValueType.Categorical:
                        if (string.IsNullOrWhiteSpace(observation.Value))
                        {
                            logger.LogDebug("Receive categorical variable, but value observed is empty: {observation}", observation);
                            return Result<DiagnoseResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Invalid observation value"));
                        }

                        observations.Add(new ClinicalObservation(variable, observation.Value));
                        break;
                }
            }

            // Check if the samples are valid
            var pathogenIds = query.MicrobialSamples.Select(x => x.Pathogen).Distinct().ToList();
            var pathogenDbCount = await context.Pathogens.CountAsync(x => pathogenIds.Contains(x.Id), cancellationToken);
            if (pathogenDbCount != pathogenIds.Count)
            {
                logger.LogDebug("Not all pathogen IDs exists in database: {Detail}", new
                {
                    PathogenDbCount = pathogenDbCount,
                    PathogenProvidedCount = pathogenIds.Count
                });
                return Result<DiagnoseResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all pathogen IDs exists in database"));
            }

            // Validate the allergies list: check if antibiotic IDs exist
            if (!query.Allergy.AllergicAntibiotics.All(a => clinicalContext.Antibiotics.Select(x => x.Id).Contains(a)))
            {
                logger.LogDebug("Allergies list contains invalid antibiotic IDs");
                return Result<DiagnoseResult>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Invalid allergies list, all IDs must be valid antibiotic IDs"));
            }

            // Construct clinical picture
            var picture = new ClinicalPicture
            {
                Observations = observations,
                Allergy = new Allergy
                {
                    Type = query.Allergy.Type,
                    AllergicAntibiotics = query.Allergy.AllergicAntibiotics.Select(a => clinicalContext.Antibiotics.First(ab => ab.Id == a)),
                    OtherAllergy = query.Allergy.OtherAllergy
                },
                OtherInfections = query.OtherInfections,
                SpecialMedicalHistory = query.SpecialMedicalHistory,
                MicrobialSamples = query.MicrobialSamples.Select(x => new MicrobialSample
                {
                    MicCategory = x.MicCategory,
                    SampleType = x.SampleType,
                    Pathogen = clinicalContext.Pathogens.First(p => p.Id == x.Pathogen)
                })
            };

            // Start severity and infection diagnosis
            logger.LogDebug("Start severity and infection assessment diagnosis");
            var diagnosisResult = service.Diagnose(clinicalContext, observations);
            if (diagnosisResult.IsFailure())
            {
                logger.LogDebug("Severity and infection diagnosis failed: {error}", diagnosisResult.Error);
                return Result<DiagnoseResult>.Failure(diagnosisResult.Error!);
            }
            var diagnosis = diagnosisResult.Data!;

            // Start microbial sample test
            var microbialResult = service.GetMicrobiologicalTestResult(clinicalContext, picture.MicrobialSamples);
            if (microbialResult.IsFailure())
            {
                logger.LogDebug("Microbialogical test diagnosis failed: {error}", microbialResult.Error);
                return Result<DiagnoseResult>.Failure(microbialResult.Error!);
            }
            var microbial = microbialResult.Data!;

            // Start recommend treatment
            var treatmentResult = service.RecommendTreatment(clinicalContext, picture, diagnosis);
            if (treatmentResult.IsFailure())
            {
                logger.LogDebug("Recommend treatment failed: {error}", treatmentResult.Error);
                return Result<DiagnoseResult>.Failure(treatmentResult.Error!);
            }
            var treatment = treatmentResult.Data!;

            return Result<DiagnoseResult>.Success(ApplicationStatus.Success, new DiagnoseResult
            {
                Severity = diagnosis.Severity,
                TreatmentSite = diagnosis.TreatmentSite,
                MetricsSeverityDiagnosis = diagnosis.MetricsSeverityDiagnosis,
                WorthSuspected = [.. diagnosis.WorthSuspected.Select(p => new PathogenResult(p.Id, p.Name))],
                HeavySuspected = [.. diagnosis.HeavySuspected.Select(p => new PathogenResult(p.Id, p.Name))],
                Evidences = [.. diagnosis.Evidences],
                MissingVariables = [.. diagnosis.MissingVariables.Select(v => new ClinicalVariableResult(v.Id, v.Name, v.Code))],
                IsPatientPregnantOrInLactationPhase = diagnosis.IsPatientPregnantOrInLactationPhase,
                Egfr = diagnosis.Egfr,
                OtherInfections = query.OtherInfections,
                MicrobialTestResult = microbial.Select(x => new AntibiogramResult
                {
                    Id = x.Id,
                    Category = x.MicCategory,
                    Pathogen = new PathogenResult(x.Pathogen.Id, x.Pathogen.Name),
                    Antibiotics = x.Antibiotics.Select(x => new AntibioticResult(x.Id, x.Name))
                }),
                Protocols = treatment.Select(x => new TreatmentProtocolResult
                {
                    Id = x.Id,
                    Severity = x.Severity,
                    TreatmentSite = x.TreatmentSite,
                    Pathogens = x.Pathogens.Select(p => new PathogenResult(p.Id, p.Name)),
                    Criteria = x.Criteria.Select(c => new CriterionResult(c.Id, c.Name, c.Formula.ToString()!)),
                    Medicines = x.Medicines.Select(mc => mc.Antibiotics.Select(a => new AntibioticResult(a.Id, a.Name)))
                })
            });
        }
    }
}
