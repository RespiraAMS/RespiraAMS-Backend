using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Features.Audit.Rollback;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Auditing;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Auditing;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.Audit;

public sealed class ClinicalRollbackServiceTest : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly DbContextOptions<ClinicalDbContext> _options;

    public ClinicalRollbackServiceTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<ClinicalDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.EnableRetryOnFailure())
            .Options;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        await using var context = new ClinicalDbContext(_options);
        await context.AuditEntries.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Treatments.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.TreatmentProtocols.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Antibiograms.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Dosages.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Antibiotics.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.AntibioticGroups.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.RiskFactors.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.SuspectedCauses.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Pathogens.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.Criteria.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await context.ClinicalVariables.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateManyToMany_RollbackRestoresRelationsAndTreatments()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var group = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Beta-lactam antibiotics",
            ParentId = null,
        };

        var amoxicillin = new Antibiotic
        {
            Name = "Amoxicillin",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Access,
        };

        var ceftriaxone = new Antibiotic
        {
            Name = "Ceftriaxone",
            AntibioticGroupId = group.Id,
            Classification = AwareClassification.Watch,
        };

        var firstPathogen = new Pathogen
        {
            Name = "Streptococcus pneumoniae",
            Description = "Pneumococcus",
            IsAtypical = false,
        };

        var secondPathogen = new Pathogen
        {
            Name = "Mycoplasma pneumoniae",
            Description = "Atypical pathogen",
            IsAtypical = true,
        };

        var firstCriterion = new Criterion
        {
            Name = "Adult",
            Formula = new BooleanConstantFormula(true),
        };

        var secondCriterion = new Criterion
        {
            Name = "No penicillin allergy",
            Formula = new BooleanConstantFormula(true),
        };

        var protocol = new TreatmentProtocol
        {
            Severity = Severity.Moderate,
            TreatmentSite = TreatmentSite.Inpatient,
            Version = "v1",
            Publisher = "Publisher",
            IsApplied = true,
            Pathogens = [firstPathogen],
            Criteria = [firstCriterion],
        };

        var originalTreatment = new Treatment
        {
            TreatmentProtocolId = protocol.Id,
            Combination = [amoxicillin],
        };

        await using (var seedContext = new ClinicalDbContext(_options))
        {
            seedContext.AddRange(
                group,
                amoxicillin,
                ceftriaxone,
                firstPathogen,
                secondPathogen,
                firstCriterion,
                secondCriterion,
                protocol,
                originalTreatment
            );
            await seedContext.SaveChangesAsync(cancellationToken);
        }

        // A new SaveChanges represents one complete change set. Clearing the
        // collections removes old many-to-many rows, adding new relations creates
        // new rows, and replacing the treatment exercises entity delete + insert.
        await using (var updateContext = new ClinicalDbContext(_options))
        {
            var trackedProtocol = await updateContext
                .TreatmentProtocols.Include(item => item.Pathogens)
                .Include(item => item.Criteria)
                .Include(item => item.Treatments)
                    .ThenInclude(item => item.Combination)
                .SingleAsync(item => item.Id == protocol.Id, cancellationToken);

            trackedProtocol.Version = "v2";
            foreach (var treatment in trackedProtocol.Treatments)
                treatment.Combination.Clear();

            trackedProtocol.Treatments.Clear();
            trackedProtocol.Pathogens.Clear();
            trackedProtocol.Criteria.Clear();
            trackedProtocol.Pathogens.Add(
                await updateContext.Pathogens.SingleAsync(
                    item => item.Id == secondPathogen.Id,
                    cancellationToken
                )
            );
            trackedProtocol.Criteria.Add(
                await updateContext.Criteria.SingleAsync(
                    item => item.Id == secondCriterion.Id,
                    cancellationToken
                )
            );

            updateContext.Treatments.Add(
                new Treatment
                {
                    TreatmentProtocolId = trackedProtocol.Id,
                    Combination =
                    [
                        await updateContext.Antibiotics.SingleAsync(
                            item => item.Id == ceftriaxone.Id,
                            cancellationToken
                        ),
                    ],
                }
            );

            await updateContext.SaveChangesAsync(cancellationToken);
        }

        Guid auditEntryId;
        Guid changeSetId;

        await using (var auditContext = new ClinicalDbContext(_options))
        {
            var updateAudit = await auditContext
                .AuditEntries.AsNoTracking()
                .Where(item => item.Action.StartsWith("Clinical.TreatmentProtocol.Updated"))
                .OrderByDescending(item => item.OccurredAt)
                .FirstAsync(cancellationToken);

            auditEntryId = updateAudit.Id;
            changeSetId = updateAudit.ChangeSetId;
        }

        await using (var rollbackContext = new ClinicalDbContext(_options))
        {
            var accessor = new Mock<IAuditContextAccessor>();
            accessor
                .Setup(item => item.GetCurrent())
                .Returns(new AuditContext(Guid.CreateVersion7(), "Admin", null, "/test", null));

            var service = new ClinicalRollbackService(
                rollbackContext,
                accessor.Object,
                Mock.Of<ILogger<ClinicalRollbackService>>()
            );

            var preview = await service.PreviewAsync(auditEntryId, cancellationToken);
            Assert.NotNull(preview);
            Assert.True(
                preview.CanApply,
                string.Join(
                    "; ",
                    preview.Conflicts.Select(item =>
                        $"{item.EntityType}/{item.EntityId}: {item.Reason}"
                    )
                )
            );
            Assert.Contains(preview.Changes, item => item.IsJoinTable);
            Assert.Contains(preview.Changes, item => item.Operation == "Created");
            Assert.Contains(preview.Changes, item => item.Operation == "Deleted");

            var result = await service.ApplyAsync(auditEntryId, cancellationToken);
            Assert.NotNull(result);
            Assert.Equal(changeSetId, result.ChangeSetId);
            Assert.True(result.ChangesApplied > 0);
        }

        await using var verifyContext = new ClinicalDbContext(_options);
        var restored = await verifyContext
            .TreatmentProtocols.Include(item => item.Pathogens)
            .Include(item => item.Criteria)
            .Include(item => item.Treatments)
            .ThenInclude(item => item.Combination)
            .SingleAsync(item => item.Id == protocol.Id, cancellationToken);

        Assert.Equal("v1", restored.Version);
        Assert.Equal(firstPathogen.Id, Assert.Single(restored.Pathogens).Id);
        Assert.Equal(firstCriterion.Id, Assert.Single(restored.Criteria).Id);

        var restoredTreatment = Assert.Single(restored.Treatments);
        Assert.Equal(originalTreatment.Id, restoredTreatment.Id);
        Assert.Equal(amoxicillin.Id, Assert.Single(restoredTreatment.Combination).Id);

        var rollbackAudit = await verifyContext.AuditEntries
            .AsNoTracking()
            .OrderByDescending(item => item.OccurredAt)
            .FirstAsync(item => item.Action == "Clinical.AuditRollback", cancellationToken);
        Assert.Equal(auditEntryId, rollbackAudit.EntityId);
    }

    [Fact]
    public async Task UpdateFormulaAndOwnedRange_RollbackRestoresSnapshots()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var criterion = new Criterion
        {
            Name = "Boolean criterion",
            Formula = new BooleanConstantFormula(true),
        };

        var variable = new NumericClinicalVariable
        {
            Code = "BUN",
            Name = "Blood urea nitrogen",
            Description = "BUN",
            IsRequired = true,
            Category = ClinicalVariableCategory.Paraclinical,
            AcceptedRange = new Range
            {
                Min = 0,
                IsMinExclusive = false,
                Max = 100,
                IsMaxExclusive = false,
                Unit = "mg/dL",
            },
            NormalRange = new Range
            {
                Min = 5,
                IsMinExclusive = false,
                Max = 25,
                IsMaxExclusive = false,
                Unit = "mg/dL",
            },
        };

        await using (var seedContext = new ClinicalDbContext(_options))
        {
            seedContext.AddRange(criterion, variable);
            await seedContext.SaveChangesAsync(cancellationToken);
        }

        await using (var updateContext = new ClinicalDbContext(_options))
        {
            var trackedCriterion = await updateContext.Criteria.SingleAsync(
                item => item.Id == criterion.Id,
                cancellationToken
            );
            trackedCriterion.Formula = new BooleanConstantFormula(false);

            var trackedVariable = (NumericClinicalVariable)
                await updateContext.ClinicalVariables.SingleAsync(
                    item => item.Id == variable.Id,
                    cancellationToken
                );
            trackedVariable.AcceptedRange = new Range
            {
                Min = 10,
                IsMinExclusive = true,
                Max = 90,
                IsMaxExclusive = true,
                Unit = "mg/dL",
            };
            trackedVariable.NormalRange = null;

            await updateContext.SaveChangesAsync(cancellationToken);
        }

        Guid auditEntryId;
        await using (var auditContext = new ClinicalDbContext(_options))
        {
            auditEntryId = await auditContext
                .AuditEntries.AsNoTracking()
                .Where(item => item.Action.StartsWith("Clinical.Criterion.Updated"))
                .OrderByDescending(item => item.OccurredAt)
                .Select(item => item.Id)
                .FirstAsync(cancellationToken);
        }

        await using (var rollbackContext = new ClinicalDbContext(_options))
        {
            var accessor = new Mock<IAuditContextAccessor>();
            accessor
                .Setup(item => item.GetCurrent())
                .Returns(new AuditContext(Guid.CreateVersion7(), "Admin", null, "/test", null));

            var service = new ClinicalRollbackService(
                rollbackContext,
                accessor.Object,
                Mock.Of<ILogger<ClinicalRollbackService>>()
            );

            var preview = await service.PreviewAsync(auditEntryId, cancellationToken);
            Assert.NotNull(preview);
            Assert.True(
                preview.CanApply,
                string.Join(
                    "; ",
                    preview.Conflicts.Select(item =>
                        $"{item.EntityType}/{item.EntityId}: {item.Reason}"
                    )
                )
            );

            var result = await service.ApplyAsync(auditEntryId, cancellationToken);
            Assert.NotNull(result);
        }

        await using var verifyContext = new ClinicalDbContext(_options);
        var restoredCriterion = await verifyContext.Criteria.SingleAsync(
            item => item.Id == criterion.Id,
            cancellationToken
        );
        var restoredFormula = Assert.IsType<BooleanConstantFormula>(restoredCriterion.Formula);
        Assert.True(restoredFormula.Constant);

        var restoredVariable = (NumericClinicalVariable)
            await verifyContext.ClinicalVariables.SingleAsync(
                item => item.Id == variable.Id,
                cancellationToken
            );
        Assert.Equal(0, restoredVariable.AcceptedRange.Min);
        Assert.False(restoredVariable.AcceptedRange.IsMinExclusive);
        Assert.Equal(100, restoredVariable.AcceptedRange.Max);
        Assert.False(restoredVariable.AcceptedRange.IsMaxExclusive);
        Assert.NotNull(restoredVariable.NormalRange);
        Assert.Equal(5, restoredVariable.NormalRange.Min);
        Assert.Equal(25, restoredVariable.NormalRange.Max);
    }

    [Fact]
    public async Task Rollback_SuppressesIntermediateAuditEntriesAndDetectsStaleConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var pathogen = new Pathogen
        {
            Name = "Klebsiella pneumoniae",
            Description = "Gram-negative rod",
            IsAtypical = false,
        };

        await using (var seedContext = new ClinicalDbContext(_options))
        {
            seedContext.Pathogens.Add(pathogen);
            await seedContext.SaveChangesAsync(cancellationToken);
        }

        await using (var firstUpdate = new ClinicalDbContext(_options))
        {
            var tracked = await firstUpdate.Pathogens.SingleAsync(
                item => item.Id == pathogen.Id,
                cancellationToken
            );
            tracked.Description = "Updated v1";
            await firstUpdate.SaveChangesAsync(cancellationToken);
        }

        Guid firstUpdateAuditId;
        int auditCountBeforeRollback;
        await using (var auditContext = new ClinicalDbContext(_options))
        {
            firstUpdateAuditId = await auditContext
                .AuditEntries.AsNoTracking()
                .Where(item => item.Action == "Clinical.Pathogen.Updated")
                .Select(item => item.Id)
                .SingleAsync(cancellationToken);
            auditCountBeforeRollback = await auditContext.AuditEntries.CountAsync(cancellationToken);
        }

        await using (var rollbackContext = new ClinicalDbContext(_options))
        {
            var accessor = new Mock<IAuditContextAccessor>();
            accessor
                .Setup(item => item.GetCurrent())
                .Returns(new AuditContext(Guid.CreateVersion7(), "Admin", null, "/test", null));

            var service = new ClinicalRollbackService(
                rollbackContext,
                accessor.Object,
                Mock.Of<ILogger<ClinicalRollbackService>>()
            );

            var result = await service.ApplyAsync(firstUpdateAuditId, cancellationToken);
            Assert.NotNull(result);
        }

        await using (var verifyContext = new ClinicalDbContext(_options))
        {
            // Only the single explicit Rollback entry should be added during ApplyAsync
            var auditCountAfterRollback = await verifyContext.AuditEntries.CountAsync(cancellationToken);
            Assert.Equal(auditCountBeforeRollback + 1, auditCountAfterRollback);

            // Modify the entity again so the original update audit entry is now stale
            var tracked = await verifyContext.Pathogens.SingleAsync(
                item => item.Id == pathogen.Id,
                cancellationToken
            );
            tracked.Description = "Diverged v2";
            await verifyContext.SaveChangesAsync(cancellationToken);
        }

        await using (var conflictContext = new ClinicalDbContext(_options))
        {
            var accessor = new Mock<IAuditContextAccessor>();
            accessor
                .Setup(item => item.GetCurrent())
                .Returns(new AuditContext(Guid.CreateVersion7(), "Admin", null, "/test", null));

            var service = new ClinicalRollbackService(
                conflictContext,
                accessor.Object,
                Mock.Of<ILogger<ClinicalRollbackService>>()
            );

            await Assert.ThrowsAsync<ClinicalRollbackConflictException>(() =>
                service.ApplyAsync(firstUpdateAuditId, cancellationToken)
            );
        }
    }
}
