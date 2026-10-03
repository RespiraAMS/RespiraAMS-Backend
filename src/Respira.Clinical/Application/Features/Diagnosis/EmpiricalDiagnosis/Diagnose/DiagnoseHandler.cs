using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Entities;
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
                Metrics = await context.ScoreMetrics
                    .AsNoTracking()
                    .Include(x => x.ScoringRules)
                    .ThenInclude(r => r.Criterion)
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

            // Internal validation of each observations
            logger.LogDebug("Validating observations by each internal rule");
            var errors = new Dictionary<Guid, string>();
            foreach (var observation in observations)
            {
                if (observation.Variable.ValueType == ClinicalValueType.Numeric && !observation.Variable.IsValidValue(observation.NumericValue))
                {
                    logger.LogInformation("Invalid observation value for variable {code}: {value}", observation.Variable.Code, observation.NumericValue);

                    // Add the error to the list
                    var variable = (NumericClinicalVariable)observation.Variable;
                    errors.Add(variable.Id, $"{variable.Name} phải {variable.AcceptedRange}");
                    continue;
                }

                if (observation.Variable.ValueType == ClinicalValueType.Boolean && !observation.Variable.IsValidValue(observation.BooleanValue))
                {
                    logger.LogInformation("Invalid observation value for variable {code}: {value}", observation.Variable.Code, observation.BooleanValue);

                    // Add the error to the list (though this case is unlikely to happen because of C# type checking)
                    var variable = (BooleanClinicalVariable)observation.Variable;
                    errors.Add(variable.Id, $"{variable.Name} should be a valid boolean value");
                    continue;
                }

                if (observation.Variable.ValueType == ClinicalValueType.Categorical && !observation.Variable.IsValidValue(observation.CategoricalValue))
                {
                    logger.LogInformation("Invalid observation value for variable {code}: {value}", observation.Variable.Code, observation.CategoricalValue);

                    // Add the error to the list
                    var variable = (CategoricalClinicalVariable)observation.Variable;
                    errors.Add(variable.Id, $"{variable.Name} should be in this value range {string.Join(", ", variable.AcceptedValues)}");
                }
            }

            // Validation: if is pregnant variable is true, check if sex if female or not
            var pregnant = observations.FirstOrDefault(x => x.Variable.Code.Equals("PREGNANT-OR-LACTATING"));
            if (pregnant is not null && pregnant.BooleanValue == true)
            {
                logger.LogDebug("Pregnant variable is true, check if sex is female");
                var isFemale = observations.FirstOrDefault(x => x.Variable.Code.Equals("FEMALE"));
                if (isFemale is null || isFemale.BooleanValue == false)
                {
                    logger.LogDebug("Sex is not female when pregnant is true");
                    errors.Add(pregnant.Variable.Id, "Sex should be female when pregnant is true");
                }
            }

            if (errors.Count > 0)
            {
                return Result<DiagnoseResult>.Failure(new Error(ApplicationStatus.BadRequest, "Invalid observation value", errors));
            }

            // Start diagnosis
            logger.LogDebug("Start severity diagnosis");
            var severityDiagnosis = service.DiagnoseSeverity(clinicalContext, observations);
            if (severityDiagnosis.IsFailure())
            {
                logger.LogDebug("Diagnose severity failed: {detail}", severityDiagnosis.Error);
                return Result<DiagnoseResult>.Failure(severityDiagnosis.Error!);
            }

            var infectionAssessment = service.AssessInfection(
                clinicalContext,
                observations,
                severityDiagnosis.Data!.Severity,
                severityDiagnosis.Data.TreatmentSite);
            if (infectionAssessment.IsFailure())
            {
                logger.LogDebug("Assess infection failed: {detail}", infectionAssessment.Error);
                return Result<DiagnoseResult>.Failure(infectionAssessment.Error!);
            }

            logger.LogDebug("Diagnose result: {detail}", new
            {
                Severity = severityDiagnosis.Data,
                Infection = infectionAssessment.Data,
            });

            var evidences = severityDiagnosis.Data.MetricsDiagnoses.SelectMany(x => x.Evidences)
                .Concat(infectionAssessment.Data!.Evidences);

            return Result<DiagnoseResult>.Success(ApplicationStatus.Success, new DiagnoseResult
            {
                SeverityDiagnosis = severityDiagnosis.Data,
                WorthSuspected = infectionAssessment.Data!.WorthSuspected.Select(x => new PathogenResult(x.Id, x.Name)),
                HeavySuspected = infectionAssessment.Data.HeavySuspected.Select(x => new ScoredPathogenResult(x.Pathogen.Id, x.Pathogen.Name, x.PriorityScore)),
                Evidences = evidences,
            });
        }
    }
}
