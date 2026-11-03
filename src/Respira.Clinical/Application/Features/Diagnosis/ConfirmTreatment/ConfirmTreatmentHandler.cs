using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

/*
 * NOTE: this is NOT an independent feature.
 *
 * This is just an extension of the diagnose feature.
 * Since the flow of diagnose is basically like this:
 * doctor -clinical picture-> server -diagnosis result-> doctor -pick medicines-> server
 * which has need actor response in a middle of the flow,
 * so we need to split the whole flow into separate handlers.
 * Because of that, it's acceptable to reference the DiagnoseResult
 * class (which normally anti-pattern in a Vertical Slice Architecture)
 *
 * Another thing to note: the validation rule for reason required
 * the evaluation of adherence, which is quite it should be checked
 * on the handler instead of validator (for UX, we can just design 
 * the client in a clever way so that the reason check can be
 * achieve in client side as well). Of course, the antibiotic group
 * rule is a domain business rule, so it must stay on backend
 * (single source of truth)
 */

namespace Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment
{
    public class ConfirmTreatmentHandler(
        IDbContext context,
        IDiagnoseService service,
        IMessageBus bus,
        ILogger<ConfirmTreatmentHandler> logger)
        : IQueryHandler<ConfirmTreatmentQuery, Result>
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerOptions.Web);

        public async Task<Result> HandleAsync(ConfirmTreatmentQuery query, CancellationToken cancellationToken = default)
        {
            // The diagnosis is client-supplied raw JSON. Read only the parts
            // needed for validation; the rest is forwarded to the patient
            // service exactly as the doctor received it.
            if (query.Diagnosis.ValueKind != JsonValueKind.Object)
            {
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Diagnosis result is required"));
            }

            ConfirmDiagnosisView? diagnosis;
            try
            {
                diagnosis = query.Diagnosis.Deserialize<ConfirmDiagnosisView>(Options);
            }
            catch (JsonException ex)
            {
                logger.LogDebug(ex, "Malformed diagnosis payload");
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Diagnosis result is malformed"));
            }

            if (diagnosis is null || diagnosis.DiagnosisId == Guid.Empty)
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BadRequest,
                    "Diagnosis result is missing the diagnosis ID"));
            }

            // Load clinical context
            var clinicalContext = new ClinicalContext
            {
                AntibioticGroups = await context.AntibioticGroups
                    .Include(x => x.Parent)
                    .ToListAsync(cancellationToken),
                Antibiograms = [],
                Antibiotics = await context.Antibiotics
                    .Include(x => x.AntibioticGroup)
                    .ToListAsync(cancellationToken),
                Metrics = [],
                Pathogens = [],
                SuspectedCauses = [],
                TreatmentProtocols = [],
                Variables = [],
            };

            // Get and validate antibiotic IDs
            var medicines = clinicalContext.Antibiotics
                .Where(x => query.Medicines.Contains(x.Id))
                .ToList();
            if (medicines.Count != query.Medicines.Count)
            {
                logger.LogDebug("Not all medicine IDs exist in database: {Detail}", new
                {
                    AntibioticDbCount = medicines.Count,
                    AntibioticProvidedCount = query.Medicines.Count
                });
                return Result.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Not all medicine IDs exist in database"));
            }

            // Check if the medicine that doctor chosen is valid.
            // A valid treatment medicine must not violate rule:
            // all antibiotics in a combination must not belong to a group
            var validateResult = service.IsMedicineCombinationValid(clinicalContext, medicines);
            if (validateResult.IsFailure())
            {
                logger.LogDebug("Failed to validate medicine combination: {Error}", validateResult.Error);
                return Result.Failure(validateResult.Error!);
            }

            if (!validateResult.Data)
            {
                return Result.Failure(new ApplicationError(
                    ApplicationStatus.BusinessRuleViolation,
                    "All medicine must belong to different antibiotic group"));
            }

            // Check if this medicines picked by doctor is adherence to treatment protocol
            // or not. Adherence is derived from the protocols carried in the
            // client-supplied diagnosis.
            var adherence = (diagnosis.Protocols ?? [])
                .SelectMany(p => p.Medicines ?? [])
                .OfType<IEnumerable<ConfirmMedicineView>>()
                .Any(combination =>
                        combination.Count() == query.Medicines.Count &&
                        combination.All(a => query.Medicines.Contains(a.Id)));
            logger.LogInformation("Is doctor medicine pick adhere to protocol: {Adhere}", adherence);

            // Validate business rule: if not adhere, then a reason must be provided
            if (!adherence && string.IsNullOrWhiteSpace(query.ReasonForMedicineChoice))
            {
                const string msg = "Medicines picked do not adhere to treatment protocol, but no reason was provided";
                logger.LogDebug(msg);
                return Result.Failure(new ApplicationError(ApplicationStatus.BusinessRuleViolation, msg));
            }

            // Create patient's treatment plan
            var result = await bus.InvokeAsync<Result<UpsertTreatmentPlanResult>>(new UpsertTreatmentPlanCommand
            {
                DoctorId = query.DoctorId,
                PatientId = query.PatientId,
                DiagnosisId = diagnosis.DiagnosisId,
                IsAdhere = adherence,
                // Forward the client's snapshot verbatim: no re-serialization,
                // so nothing the doctor saw is lost or reshaped on the way.
                Diagnosis = query.Diagnosis.GetRawText(),
                Medicines = [.. medicines.Select(x => new AntibioticQuery(x.Id, x.Name, x.Classification))],
                ReasonForMedicineChoice = query.ReasonForMedicineChoice
            });

            if (result.IsFailure())
            {
                logger.LogDebug("Failed to create treatment plan: {Error}", result.Error);
                return Result.Failure(new ApplicationError(
                    result.Error!.StatusCode,
                    $"Failed to create patient's treatment plan: {result.Error!.Description}"));
            }

            return Result.Success(ApplicationStatus.Success);
        }
    }
}
