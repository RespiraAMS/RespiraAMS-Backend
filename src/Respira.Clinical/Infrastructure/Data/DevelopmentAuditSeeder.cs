using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Auditing;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Infrastructure.Data;

/// <summary>
/// Seeds deterministic Clinical audit log &amp; rollback test scenarios in Development.
/// Safe and idempotent to run even when the Clinical database already contains initial seed data.
/// </summary>
public static class DevelopmentAuditSeeder
{
    public static readonly Guid AdminActorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid ReviewerActorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // Deterministic Audit Entry IDs for quick testing in Swagger / Postman
    public static readonly Guid ScalarUpdateAuditId = Guid.Parse("90000000-0000-0000-0000-000000000001");
    public static readonly Guid FormulaAndRangeUpdateAuditId = Guid.Parse("90000000-0000-0000-0000-000000000002");
    public static readonly Guid ManyToManyProtocolUpdateAuditId = Guid.Parse("90000000-0000-0000-0000-000000000003");
    public static readonly Guid SoftDeleteRiskFactorAuditId = Guid.Parse("90000000-0000-0000-0000-000000000004");
    public static readonly Guid StaleConflictUpdateAuditId = Guid.Parse("90000000-0000-0000-0000-000000000005");
    public static readonly Guid LatestSuspectedCauseUpdateAuditId = Guid.Parse("90000000-0000-0000-0000-000000000006");

    // Deterministic Entity IDs
    public static readonly Guid PathogenOneId = Guid.Parse("80000000-0000-0000-0000-000000000001");
    public static readonly Guid PathogenTwoId = Guid.Parse("80000000-0000-0000-0000-000000000010");
    public static readonly Guid CriterionOneId = Guid.Parse("80000000-0000-0000-0000-000000000002");
    public static readonly Guid CriterionTwoId = Guid.Parse("80000000-0000-0000-0000-000000000011");
    public static readonly Guid NumericVariableId = Guid.Parse("80000000-0000-0000-0000-000000000003");
    public static readonly Guid ProtocolId = Guid.Parse("80000000-0000-0000-0000-000000000004");
    public static readonly Guid RiskFactorId = Guid.Parse("80000000-0000-0000-0000-000000000005");
    public static readonly Guid SuspectedCauseId = Guid.Parse("80000000-0000-0000-0000-000000000006");
    public static readonly Guid AntibioticGroupId = Guid.Parse("80000000-0000-0000-0000-000000000007");
    public static readonly Guid AntibioticOneId = Guid.Parse("80000000-0000-0000-0000-000000000008");
    public static readonly Guid AntibioticTwoId = Guid.Parse("80000000-0000-0000-0000-000000000009");
    public static readonly Guid InitialTreatmentId = Guid.Parse("80000000-0000-0000-0000-000000000012");
    public static readonly Guid UpdatedTreatmentId = Guid.Parse("80000000-0000-0000-0000-000000000013");

    public static async Task SeedAuditDemoDataAsync(ClinicalDbContext context, ILogger logger)
    {
        if (await context.AuditEntries.AnyAsync(entry => entry.Id == ScalarUpdateAuditId))
        {
            return;
        }

        // Clean up any partial demo entities if they existed without the audit entry
        var demoPathogenExists = await context.Pathogens
            .IgnoreQueryFilters()
            .AnyAsync(item => item.Id == PathogenOneId);
        if (demoPathogenExists)
        {
            return;
        }

        var group = new AntibioticGroup
        {
            Id = AntibioticGroupId,
            Name = "Audit Demo Beta-Lactams",
            Description = "Demo group for audit & rollback testing",
            ParentId = null,
        };

        var antibioticOne = new Antibiotic
        {
            Id = AntibioticOneId,
            Name = "Audit Demo Amoxicillin",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Access,
        };

        var antibioticTwo = new Antibiotic
        {
            Id = AntibioticTwoId,
            Name = "Audit Demo Ceftriaxone",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Watch,
        };

        var pathogenOne = new Pathogen
        {
            Id = PathogenOneId,
            Name = "Audit Demo Klebsiella pneumoniae",
            Description = "v1 - Original description before update",
            IsAtypical = false,
        };

        var pathogenTwo = new Pathogen
        {
            Id = PathogenTwoId,
            Name = "Audit Demo Mycoplasma pneumoniae",
            Description = "Atypical pathogen for protocol swap",
            IsAtypical = true,
        };

        var criterionOne = new Criterion
        {
            Id = CriterionOneId,
            Name = "Audit Demo Hypoxemia Criterion",
            Formula = new BooleanConstantFormula(true),
        };

        var criterionTwo = new Criterion
        {
            Id = CriterionTwoId,
            Name = "Audit Demo Inpatient Criterion",
            Formula = new BooleanConstantFormula(true),
        };

        var numericVariable = new NumericClinicalVariable
        {
            Id = NumericVariableId,
            Code = "AUDIT_SPO2",
            Name = "Audit Demo Peripheral Oxygen Saturation",
            Description = "SpO2 variable for testing owned Range rollback",
            CanonicalUnit = "%",
            IsRequired = false,
            Category = ClinicalVariableCategory.Clinical,
            AcceptedRange = new Range
            {
                Min = 50,
                IsMinExclusive = false,
                Max = 100,
                IsMaxExclusive = false,
                Unit = "%",
            },
            NormalRange = new Range
            {
                Min = 95,
                IsMinExclusive = false,
                Max = 100,
                IsMaxExclusive = false,
                Unit = "%",
            },
        };

        var protocol = new TreatmentProtocol
        {
            Id = ProtocolId,
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
            Version = "v1.0-audit-demo",
            Publisher = "Respira Audit Seeder",
            IsApplied = false,
            Pathogens = [pathogenOne],
            Criteria = [criterionOne],
        };

        var initialTreatment = new Treatment
        {
            Id = InitialTreatmentId,
            TreatmentProtocolId = protocol.Id,
            Combination = [antibioticOne],
        };

        var riskFactor = new RiskFactor
        {
            Id = RiskFactorId,
            PathogenId = pathogenOne.Id,
            CriterionId = criterionOne.Id,
            IsCriticalFactor = true,
        };

        var suspectedCause = new SuspectedCause
        {
            Id = SuspectedCauseId,
            PathogenId = pathogenOne.Id,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
        };

        // Step 0: Seed initial baseline entities
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    AdminActorId,
                    "Admin",
                    Guid.Parse("99000000-0000-0000-0000-000000000000"),
                    "/api/v1/clinical/seed-baseline",
                    "seed-clinical-baseline"
                )
            )
        )
        {
            context.AddRange(
                group,
                antibioticOne,
                antibioticTwo,
                pathogenOne,
                pathogenTwo,
                criterionOne,
                criterionTwo,
                numericVariable,
                protocol,
                initialTreatment,
                riskFactor,
                suspectedCause
            );
            await context.SaveChangesAsync();
        }

        // Scenario 1: Scalar Update on Pathogen (Ready to Rollback)
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    AdminActorId,
                    "Admin",
                    Guid.Parse("99000000-0000-0000-0000-000000000001"),
                    $"/api/v1/clinical/pathogens/{PathogenOneId}",
                    "seed-clinical-scalar-update"
                ),
                preferredAuditEntryId: ScalarUpdateAuditId,
                preferredChangeSetId: Guid.Parse("91000000-0000-0000-0000-000000000001")
            )
        )
        {
            pathogenOne.Description = "v2 - Updated description (ready for rollback test)";
            pathogenOne.IsAtypical = true;
            pathogenOne.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        // Scenario 2: Polymorphic Formula + Owned Range Update in one ChangeSet (Ready to Rollback)
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    ReviewerActorId,
                    "MedicalRegulationReviewer",
                    Guid.Parse("99000000-0000-0000-0000-000000000002"),
                    $"/api/v1/clinical/criteria/{CriterionOneId}",
                    "seed-clinical-formula-range-update"
                ),
                preferredAuditEntryId: FormulaAndRangeUpdateAuditId,
                preferredChangeSetId: Guid.Parse("91000000-0000-0000-0000-000000000002")
            )
        )
        {
            criterionOne.Formula = new BooleanConstantFormula(false);
            criterionOne.UpdatedAt = DateTimeOffset.UtcNow;

            numericVariable.AcceptedRange = new Range
            {
                Min = 70,
                IsMinExclusive = true,
                Max = 100,
                IsMaxExclusive = false,
                Unit = "%",
            };
            numericVariable.NormalRange = null;
            numericVariable.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();
        }

        // Scenario 3: Many-to-Many TreatmentProtocol & Treatments Update (Ready to Rollback)
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    AdminActorId,
                    "Admin",
                    Guid.Parse("99000000-0000-0000-0000-000000000003"),
                    $"/api/v1/clinical/treatment-protocols/{ProtocolId}",
                    "seed-clinical-m2m-update"
                ),
                preferredAuditEntryId: ManyToManyProtocolUpdateAuditId,
                preferredChangeSetId: Guid.Parse("91000000-0000-0000-0000-000000000003")
            )
        )
        {
            protocol.Version = "v2.0-audit-demo";
            protocol.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var treatment in protocol.Treatments)
            {
                treatment.Combination.Clear();
            }

            protocol.Treatments.Clear();
            protocol.Pathogens.Clear();
            protocol.Criteria.Clear();
            protocol.Pathogens.Add(pathogenTwo);
            protocol.Criteria.Add(criterionTwo);

            context.Treatments.Add(
                new Treatment
                {
                    Id = UpdatedTreatmentId,
                    TreatmentProtocolId = protocol.Id,
                    Combination = [antibioticTwo],
                }
            );

            await context.SaveChangesAsync();
        }

        // Scenario 4: Soft-Deleted RiskFactor (Ready to Rollback)
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    AdminActorId,
                    "Admin",
                    Guid.Parse("99000000-0000-0000-0000-000000000004"),
                    $"/api/v1/clinical/risk-factors/{RiskFactorId}",
                    "seed-clinical-soft-delete"
                ),
                preferredAuditEntryId: SoftDeleteRiskFactorAuditId,
                preferredChangeSetId: Guid.Parse("91000000-0000-0000-0000-000000000004")
            )
        )
        {
            riskFactor.IsDeleted = true;
            riskFactor.DeletedAt = DateTimeOffset.UtcNow;
            riskFactor.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        // Scenario 5a: First update on SuspectedCause (Stale entry -> will trigger 409 Conflict when rolled back)
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    ReviewerActorId,
                    "MedicalRegulationReviewer",
                    Guid.Parse("99000000-0000-0000-0000-000000000005"),
                    $"/api/v1/clinical/suspected-causes/{SuspectedCauseId}",
                    "seed-clinical-conflict-v1"
                ),
                preferredAuditEntryId: StaleConflictUpdateAuditId,
                preferredChangeSetId: Guid.Parse("91000000-0000-0000-0000-000000000005")
            )
        )
        {
            suspectedCause.Severity = Severity.Moderate;
            suspectedCause.TreatmentSite = TreatmentSite.Inpatient;
            suspectedCause.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        // Scenario 5b: Second update on SuspectedCause (Latest entry -> makes 5a stale, while 5b itself can be rolled back)
        using (
            AuditContextScope.Begin(
                new AuditContext(
                    AdminActorId,
                    "Admin",
                    Guid.Parse("99000000-0000-0000-0000-000000000006"),
                    $"/api/v1/clinical/suspected-causes/{SuspectedCauseId}",
                    "seed-clinical-conflict-v2"
                ),
                preferredAuditEntryId: LatestSuspectedCauseUpdateAuditId,
                preferredChangeSetId: Guid.Parse("91000000-0000-0000-0000-000000000006")
            )
        )
        {
            suspectedCause.Severity = Severity.Severe;
            suspectedCause.TreatmentSite = TreatmentSite.IntensiveCareUnit;
            suspectedCause.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        logger.LogInformation(
            "Seeded Clinical Audit & Rollback demo entries: ScalarUpdate={ScalarUpdateAuditId}, FormulaAndRange={FormulaAndRangeUpdateAuditId}, ManyToMany={ManyToManyProtocolUpdateAuditId}, SoftDelete={SoftDeleteRiskFactorAuditId}, StaleConflict={StaleConflictUpdateAuditId}, LatestUpdate={LatestSuspectedCauseUpdateAuditId}",
            ScalarUpdateAuditId,
            FormulaAndRangeUpdateAuditId,
            ManyToManyProtocolUpdateAuditId,
            SoftDeleteRiskFactorAuditId,
            StaleConflictUpdateAuditId,
            LatestSuspectedCauseUpdateAuditId
        );
    }
}
