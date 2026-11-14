using System.Text.Json;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Shared;
using Respira.Analysis.Domain.Entities;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Messages.Patients;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Analysis.Application.Features.Commands.Create;

public sealed class CreateAnalysisReportCommandHandler(IMessageBus bus, IAnalysisDbContext context)
    : ICommandHandler<CreateAnalysisReportCommand, Result<AnalysisReportResult>>
{
    private sealed record PlanRow(
        Guid PatientId,
        Guid PlanId,
        Guid DoctorId,
        string AdherenceState,
        IEnumerable<AntibioticAnalysisItem> Medicines
    );

    private sealed record PlanMedicine(PlanRow Plan, AntibioticAnalysisItem Medicine);

    public async Task<Result<AnalysisReportResult>> HandleAsync(
        CreateAnalysisReportCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (
            command.Start >= command.End
            || command.DoctorId == Guid.Empty
        )
        {
            return Result<AnalysisReportResult>.Failure(
                new ApplicationError(
                    ApplicationStatus.BadRequest,
                    "Start must be before end, and optional doctor ID must be valid"
                )
            );
        }

        var start = command.Start.ToUniversalTime();
        var end = command.End.ToUniversalTime();
        var sourceResult = await bus.InvokeAsync<Result<PatientsByDoctorResult>>(
            new GetPatientsByDoctorQuery
            {
                DoctorId = command.DoctorId,
                Start = start,
                End = end,
            },
            cancellationToken
        );

        var source = sourceResult?.IsSuccess() == true ? sourceResult.Data : null;
        if (source?.Patients is null)
        {
            return Result<AnalysisReportResult>.Failure(
                new ApplicationError(
                    ApplicationStatus.ServerError,
                    "Patient service did not return analysis source data"
                )
            );
        }

        var patients = source.Patients.ToList();
        var plans = patients
            .SelectMany(patient =>
                patient.TreatmentPlans.Select(plan => new PlanRow(
                    patient.Id,
                    plan.Id,
                    plan.DoctorId,
                    plan.AdherenceState,
                    plan.Medicines
                ))
            )
            .GroupBy(item => item.PlanId)
            .Select(group => group.First())
            .ToList();

        var adherentCount = plans.Count(plan =>
            plan.AdherenceState == PatientAnalysisConstants.AdherenceStates.Adhere
        );
        var nonAdherentCount = plans.Count(plan => IsNonAdherent(plan.AdherenceState));
        var pendingReviewCount = plans.Count(plan =>
            plan.AdherenceState == PatientAnalysisConstants.AdherenceStates.NotAdhere
        );
        var deceasedCount = patients.Count(patient =>
            patient.TreatmentOutcome == PatientAnalysisConstants.TreatmentOutcomes.Death
        );
        var recoveredCount = patients.Count(patient =>
            patient.TreatmentOutcome == PatientAnalysisConstants.TreatmentOutcomes.Recovered
        );

        var medicineOccurrences = plans
            .SelectMany(plan =>
                plan.Medicines.GroupBy(medicine => medicine.Id)
                    .Select(group => new PlanMedicine(plan, group.First()))
            )
            .ToList();

        var doctors = plans
            .GroupBy(plan => plan.DoctorId)
            .Select(doctorGroup =>
            {
                var doctorPlanIds = doctorGroup.Select(plan => plan.PlanId).ToHashSet();
                var doctorMedicines = medicineOccurrences
                    .Where(item => doctorPlanIds.Contains(item.Plan.PlanId))
                    .GroupBy(item => item.Medicine.Id)
                    .Select(group => new MedicineAnalysisSummary(
                        group.Key,
                        group.First().Medicine.Name,
                        group.First().Medicine.Classification,
                        group.Select(item => item.Plan.PlanId).Distinct().Count(),
                        group
                            .Where(item =>
                                item.Plan.AdherenceState
                                == PatientAnalysisConstants.AdherenceStates.Adhere
                            )
                            .Select(item => item.Plan.PlanId)
                            .Distinct()
                            .Count(),
                        group
                            .Where(item => IsNonAdherent(item.Plan.AdherenceState))
                            .Select(item => item.Plan.PlanId)
                            .Distinct()
                            .Count(),
                        group
                            .Where(item =>
                                item.Plan.AdherenceState
                                == PatientAnalysisConstants.AdherenceStates.NotAdhere
                            )
                            .Select(item => item.Plan.PlanId)
                            .Distinct()
                            .Count()
                    ))
                    .OrderBy(item => item.Name)
                    .ToList();

                return new DoctorMedicineAnalysisSummary(
                    doctorGroup.Key,
                    doctorGroup.Select(plan => plan.PatientId).Distinct().Count(),
                    doctorGroup.Count(),
                    doctorGroup.Count(plan =>
                        plan.AdherenceState == PatientAnalysisConstants.AdherenceStates.Adhere
                    ),
                    doctorGroup.Count(plan => IsNonAdherent(plan.AdherenceState)),
                    doctorGroup.Count(plan =>
                        plan.AdherenceState == PatientAnalysisConstants.AdherenceStates.NotAdhere
                    ),
                    doctorMedicines
                );
            })
            .OrderBy(item => item.DoctorId)
            .ToList();

        var analysis = new ClinicalAnalysis
        {
            DoctorId = command.DoctorId,
            Start = start,
            End = end,
            PatientIdsJson = JsonSerializer.Serialize(
                patients.Select(patient => patient.Id).Distinct(),
                AnalysisReportReader.JsonOptions
            ),
            TreatmentPlanIdsJson = JsonSerializer.Serialize(
                plans.Select(item => item.PlanId).Distinct(),
                AnalysisReportReader.JsonOptions
            ),
        };

        var result = new AnalysisReportResult(
            analysis.Id,
            command.DoctorId,
            start,
            end,
            plans.Count,
            patients.Count,
            adherentCount,
            nonAdherentCount,
            pendingReviewCount,
            deceasedCount,
            recoveredCount,
            patients.Count - deceasedCount - recoveredCount,
            doctors
        );

        analysis.ReportJson = JsonSerializer.Serialize(result, AnalysisReportReader.JsonOptions);
        await context.ClinicalAnalyses.AddAsync(analysis, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<AnalysisReportResult>.Success(ApplicationStatus.Created, result);
    }

    private static bool IsNonAdherent(string state) =>
        state
            is PatientAnalysisConstants.AdherenceStates.NotAdhereValid
                or PatientAnalysisConstants.AdherenceStates.NotAdhereInvalid;
}
