using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Diagnosis.EmpiricalDiagnosis.Diagnose
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
                    .ToListAsync(),
                Pathogens = await context.Pathogens
                    .AsNoTracking()
                    .Include(x => x.RiskFactors)
                    .ThenInclude(f => f.Criterion)
                    .ToListAsync(),
                SuspectedCauses = await context.SuspectedCauses
                    .AsNoTracking()
                    .Include(x => x.Pathogen)
                    .ThenInclude(p => p.RiskFactors)
                    .ThenInclude(f => f.Criterion)
                    .ToListAsync(),
                Metrics = await context.ClinicalMetrics
                    .AsNoTracking()
                    .Include(x => x.Rules)
                    .ThenInclude(r => r.Criterion)
                    .ToListAsync(),
                AntibioticGroups = await context.AntibioticGroups
                    .AsNoTracking()
                    .Include(x => x.Parent)
                    .ToListAsync(),
                Antibiotics = await context.Antibiotics
                    .AsNoTracking()
                    .Include(x => x.AntibioticGroup)
                    .Include(x => x.Dosages)
                    .ToListAsync(),
            };

            logger.LogDebug("Clinical context loaded: {detail}", new
            {
                VariablesCount = clinicalContext.Variables.Count(),
                PathogensCount = clinicalContext.Pathogens.Count(),
                SuspectedCausesCount = clinicalContext.SuspectedCauses.Count(),
                MetricsCount = clinicalContext.Metrics.Count(),
            });

            // Validate the observations: check if ID exists and value match with variable
            var observations = new List<ClinicalObservation>();
            foreach (var observation in query.Observations)
            {
                if (!clinicalContext.Variables.Any(x => x.Id == observation.VariableId))
                {
                    logger.LogDebug("Variable {variableId} not found in clinical context", observation.VariableId);
                    return Result<DiagnoseResult>.Failure(new Error(ApplicationStatus.BadRequest, "Variable not found"));
                }

                var variable = clinicalContext.Variables.First(x => x.Id == observation.VariableId);
                switch (variable.ValueType)
                {
                    case ClinicalValueType.Boolean:
                        if (!bool.TryParse(observation.Value, out var value))
                        {
                            logger.LogDebug("Receive boolean variable, but value observed is not boolean: {observation}", observation);
                            return Result<DiagnoseResult>.Failure(new Error(ApplicationStatus.BadRequest, "Invalid observation value"));
                        }

                        observations.Add(new ClinicalObservation(variable, value));
                        break;
                    case ClinicalValueType.Numeric:
                        if (!decimal.TryParse(observation.Value, out var numericValue))
                        {
                            logger.LogDebug("Receive numeric variable, but value observed is not numeric: {observation}", observation);
                            return Result<DiagnoseResult>.Failure(new Error(ApplicationStatus.BadRequest, "Invalid observation value"));
                        }

                        observations.Add(new ClinicalObservation(variable, numericValue));
                        break;
                    case ClinicalValueType.Categorical:
                        if (string.IsNullOrWhiteSpace(observation.Value))
                        {
                            logger.LogDebug("Receive categorical variable, but value observed is empty: {observation}", observation);
                            return Result<DiagnoseResult>.Failure(new Error(ApplicationStatus.BadRequest, "Invalid observation value"));
                        }

                        observations.Add(new ClinicalObservation(variable, observation.Value));
                        break;
                }
            }

            // Validate the allergies list: check if antibiotic IDs exist
            if (!query.Allergies.All(a => clinicalContext.Antibiotics.Select(x => x.Id).Contains(a)))
            {
                logger.LogDebug("Allergies list contains invalid antibiotic IDs");
                return Result<DiagnoseResult>.Failure(new Error(ApplicationStatus.BadRequest, "Invalid allergies list, all IDs must be valid antibiotic IDs"));
            }

            // Construct clinical picture
            var picture = new ClinicalPicture
            {
                Observations = observations,
                Allergies = [.. query.Allergies.Select(a => clinicalContext.Antibiotics.First(ab => ab.Id == a))],
            };

            // Start diagnosis
            logger.LogDebug("Start diagnosis");
            var diagnosisResult = service.Diagnose(clinicalContext, picture);
            if (diagnosisResult.IsFailure())
            {
                logger.LogDebug("Diagnosis failed: {error}", diagnosisResult.Error);
                return Result<DiagnoseResult>.Failure(diagnosisResult.Error!);
            }

            return Result<DiagnoseResult>.Success(ApplicationStatus.Success, new DiagnoseResult
            {
                Severity = diagnosisResult.Data!.Severity,
                TreatmentSite = diagnosisResult.Data.TreatmentSite,
                WorthSuspected = [.. diagnosisResult.Data.WorthSuspected.Select(p => new PathogenResult(p.Id, p.Name))],
                HeavySuspected = [.. diagnosisResult.Data.HeavySuspected.Select(p => new PathogenResult(p.Id, p.Name))],
                Evidences = [.. diagnosisResult.Data.Evidences],
                MissingVariables = [.. diagnosisResult.Data.MissingVariables.Select(v => new ClinicalVariableResult(v.Id, v.Name, v.Code))],
                Allergies = [.. diagnosisResult.Data.Allergies.Select(a => new AntibioticResult(a.Id, a.Name))],
                IsPatientPregnantOrInLactationPhase = diagnosisResult.Data.IsPatientPregnantOrInLactationPhase,
            });
        }
    }
}
