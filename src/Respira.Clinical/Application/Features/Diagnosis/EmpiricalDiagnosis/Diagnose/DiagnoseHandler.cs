using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Application.Contracts.Data;
using Respira.Domain.Enums;
using Respira.Domain.Models;
using Respira.Domain.Services;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Features.Diagnosis.EmpiricalDiagnosis.Diagnose
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
                }
            }

            // Start diagnosis
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

            return Result<DiagnoseResult>.Success(ApplicationStatus.Success, new DiagnoseResult
            {
                Severity = severityDiagnosis.Data.Severity,
                TreatmentSite = severityDiagnosis.Data.TreatmentSite,
                WorthSuspected = infectionAssessment.Data!.WorthSuspected.Select(x => new PathogenResult(x.Id, x.Name)),
                HeavySuspected = infectionAssessment.Data.HeavySuspected.Select(x => new ScoredPathogenResult(x.Pathogen.Id, x.Pathogen.Name, x.PriorityScore))
            });
        }
    }
}
