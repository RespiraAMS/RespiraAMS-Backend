using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.TreatmentProtocols.UpdateTreatmentProtocol
{
    public class UpdateTreatmentProtocolHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdateTreatmentProtocolHandler _handler;
        private readonly IDbContext _context;

        // Modification timestamp of the seeded protocol, far enough in the past that
        // the mapper's DateTimeOffset.UtcNow stamp is unambiguously newer
        private static readonly DateTimeOffset SeedUpdatedAt =
            new(2025, 11, 20, 8, 15, 0, TimeSpan.Zero);

        public UpdateTreatmentProtocolHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdateTreatmentProtocolMapper();
            var service = new DiagnoseService(new Mock<ILogger<DiagnoseService>>().Object);
            var logger = new Mock<ILogger<UpdateTreatmentProtocolHandler>>().Object;

            // Initialize handler
            _handler = new(_context, service, mapper, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the count
            // assertions are deterministic across runs. IgnoreQueryFilters is needed
            // because soft-deleted rows are hidden by the query filter but still
            // occupy the table
            await _context.Treatments.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.TreatmentProtocols.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.RiskFactors.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.MetricRules.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.SuspectedCauses.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Antibiograms.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Dosages.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Antibiotics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            // Antibiotic groups reference each other through ParentId, so delete
            // sub-groups before their parents
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .Where(x => x.ParentId != null)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Criteria.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        /*
         * Reference data is written through its own context and saved before the handler
         * runs, so the handler context starts clean just like a real HTTP request. This
         * matters because the handler resolves antibiotics through the database, and a
         * pre-populated change tracker would mask navigation-loading problems.
         */
        private async Task SeedAsync(params object[] entities)
        {
            await using var seedContext = new ClinicalDbContext(_options);
            seedContext.AddRange(entities);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        /*
         * Seeds a protocol that already has one medicine composition, one pathogen and
         * one criterion, which is the state every update has to replace
         */
        private async Task<TreatmentProtocol> SeedProtocolAsync(
            AntibioticGroup group,
            Antibiotic antibiotic,
            Pathogen? pathogen = null,
            Criterion? criterion = null,
            bool softDeleted = false)
        {
            var protocol = new TreatmentProtocol
            {
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
                Version = "2024.1",
                Publisher = "Bộ Y tế Việt Nam",
                IsApplied = false,
                UpdatedAt = SeedUpdatedAt,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };

            var treatment = new Treatment
            {
                TreatmentProtocolId = protocol.Id,
                Combination = [antibiotic],
            };
            protocol.Treatments = [treatment];

            if (pathogen is not null)
            {
                protocol.Pathogens = [pathogen];
            }

            if (criterion is not null)
            {
                protocol.Criteria = [criterion];
            }

            await using var seedContext = new ClinicalDbContext(_options);
            seedContext.Add(group);
            seedContext.Add(antibiotic);
            if (pathogen is not null) seedContext.Add(pathogen);
            if (criterion is not null) seedContext.Add(criterion);
            seedContext.Add(protocol);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return protocol;
        }

        private static AntibioticGroup Group(string name, Guid? parentId = null, bool softDeleted = false)
        {
            return new AntibioticGroup
            {
                Name = name,
                Description = $"{name} antibiotic group",
                ParentId = parentId,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        private static Antibiotic Antibiotic(
            string name, Guid groupId, AwareClassification classification, bool softDeleted = false)
        {
            return new Antibiotic
            {
                Name = name,
                AntibioticGroupId = groupId,
                Classification = classification,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        private static Pathogen Pathogen(string name, bool isAtypical, bool softDeleted = false)
        {
            return new Pathogen
            {
                Name = name,
                Description = $"{name} pathogen",
                IsAtypical = isAtypical,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        private static Criterion Criterion(string name, bool softDeleted = false)
        {
            return new Criterion
            {
                Name = name,
                Formula = new BooleanConstantFormula(true),
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        private async Task<TreatmentProtocol> LoadProtocolAsync(Guid id)
        {
            // Fresh context so the handler's change tracker cannot mask a failed commit
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.TreatmentProtocols
                .Include(x => x.Pathogens)
                .Include(x => x.Criteria)
                .Include(x => x.Treatments)
                    .ThenInclude(x => x.Combination)
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        /*
         * Asserts that a failed update left every persisted column and relation of the
         * protocol untouched. The handler mutates the tracked entity before validating
         * the medicine combination, so a missing SaveChanges rollback would show up here
         */
        private async Task AssertProtocolNotModifiedAsync(TreatmentProtocol original)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            var saved = await freshContext.TreatmentProtocols
                .IgnoreQueryFilters()
                .Include(x => x.Pathogens)
                .Include(x => x.Criteria)
                .Include(x => x.Treatments)
                    .ThenInclude(x => x.Combination)
                .SingleAsync(x => x.Id == original.Id, TestContext.Current.CancellationToken);

            Assert.Equal(original.Severity, saved.Severity);
            Assert.Equal(original.TreatmentSite, saved.TreatmentSite);
            Assert.Equal(original.Version, saved.Version);
            Assert.Equal(original.Publisher, saved.Publisher);
            Assert.Equal(original.IsApplied, saved.IsApplied);
            Assert.Equal(original.IsDeleted, saved.IsDeleted);
            Assert.Equal(
                original.UpdatedAt.ToUniversalTime(), saved.UpdatedAt.ToUniversalTime());

            Assert.Equal(
                original.Pathogens.Select(x => x.Id).Order(),
                saved.Pathogens.Select(x => x.Id).Order());
            Assert.Equal(
                original.Criteria.Select(x => x.Id).Order(),
                saved.Criteria.Select(x => x.Id).Order());

            Assert.Equal(
                original.Treatments.Select(x => x.Id).Order(),
                saved.Treatments.Select(x => x.Id).Order());
            Assert.Equal(
                original.Treatments
                    .Select(x => CompositionKey(x.Combination.Select(a => a.Id)))
                    .Order(),
                saved.Treatments
                    .Select(x => CompositionKey(x.Combination.Select(a => a.Id)))
                    .Order());
        }

        private async Task<int> CountTreatmentsAsync()
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.Treatments
                .IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken);
        }

        private static string CompositionKey(IEnumerable<Guid> antibioticIds)
        {
            return string.Join('|', antibioticIds.Order());
        }

        private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
        {
            // 1 .NET tick = 100 ns, 1 PostgreSQL microsecond = 10 ticks
            return new DateTimeOffset(value.Ticks - (value.Ticks % 10), value.Offset);
        }

        #endregion

        #region Happy path

        // Boundary values of the enum fields: the lowest and highest defined members
        [Theory]
        [InlineData(Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public async Task UpdateTreatmentProtocol_MinimalRelations_Success(Severity severity, TreatmentSite treatmentSite)
        {
            /*
             * Boundary shape: the updated protocol has 1 medicine composition with exactly
             * 1 antibiotic and no pathogens or criteria (0 items), so every old relation
             * must be removed
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var oldAntibiotic = Antibiotic(
                "Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var oldPathogen = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var oldCriterion = Criterion("Patient is an adult");

            await SeedAsync(betaLactams);
            var protocol = await SeedProtocolAsync(
                cephalosporins, oldAntibiotic, oldPathogen, oldCriterion);

            var aminopenicillins = Group("Aminopenicillins");
            var amoxicillin = Antibiotic(
                "Amoxicillin", aminopenicillins.Id, AwareClassification.Access);
            await SeedAsync(aminopenicillins, amoxicillin);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = severity,
                    TreatmentSite = treatmentSite,
                    Medicines = [[amoxicillin.Id]],
                    Pathogens = [],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var saved = await LoadProtocolAsync(protocol.Id);

            // Business rule: every field of the command is persisted
            Assert.Equal(severity, saved.Severity);
            Assert.Equal(treatmentSite, saved.TreatmentSite);
            Assert.Equal("2026.1", saved.Version);
            Assert.Equal("Hiệp hội Lồng ngực Anh (BTS)", saved.Publisher);
            Assert.True(saved.IsApplied);
            // PostgreSQL stores timestamps with microsecond precision, so compare at
            // that same precision to verify the creation time is untouched
            Assert.Equal(
                TruncateToMicroseconds(protocol.CreatedAt),
                TruncateToMicroseconds(saved.CreatedAt));
            Assert.True(saved.UpdatedAt.ToUniversalTime() > SeedUpdatedAt);

            // The old composition is replaced, not appended
            var treatment = Assert.Single(saved.Treatments);
            Assert.Equal(saved.Id, treatment.TreatmentProtocolId);
            var antibiotic = Assert.Single(treatment.Combination);
            Assert.Equal(amoxicillin.Id, antibiotic.Id);
            Assert.Equal(1, await CountTreatmentsAsync());

            // Empty lists must clear the old relations
            Assert.Empty(saved.Pathogens);
            Assert.Empty(saved.Criteria);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_ReplacesAllRelations_Success()
        {
            /*
             * Boundary counts above the minimum: 2 medicine compositions, 2 pathogens
             * and 2 criteria, while the seeded protocol only has 1 of each. Everything
             * from the command must replace the old state
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var oldAntibiotic = Antibiotic(
                "Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var oldPathogen = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var oldCriterion = Criterion("Patient is an adult");

            await SeedAsync(betaLactams);
            var protocol = await SeedProtocolAsync(
                cephalosporins, oldAntibiotic, oldPathogen, oldCriterion);

            var carbapenems = Group("Carbapenems", betaLactams.Id);
            var macrolides = Group("Macrolides");
            var meropenem = Antibiotic(
                "Meropenem", carbapenems.Id, AwareClassification.Watch);
            var azithromycin = Antibiotic(
                "Azithromycin", macrolides.Id, AwareClassification.Watch);

            var pseudomonas = Pathogen("Pseudomonas aeruginosa", isAtypical: false);
            var mycoplasma = Pathogen("Mycoplasma pneumoniae", isAtypical: true);
            var noPenicillinAllergy = Criterion("No penicillin allergy");
            var renalImpairment = Criterion("Creatinine clearance below 30 mL/min");

            await SeedAsync(
                carbapenems, macrolides, meropenem, azithromycin,
                pseudomonas, mycoplasma, noPenicillinAllergy, renalImpairment);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                    Medicines =
                    [
                        [meropenem.Id],
                        [azithromycin.Id],
                    ],
                    Pathogens = [pseudomonas.Id, mycoplasma.Id],
                    Criteria = [noPenicillinAllergy.Id, renalImpairment.Id],
                    Version = "2002-2003",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var saved = await LoadProtocolAsync(protocol.Id);

            Assert.Equal(Severity.Severe, saved.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, saved.TreatmentSite);
            Assert.Equal("2002-2003", saved.Version);
            Assert.Equal("Hiệp hội Lồng ngực Anh (BTS)", saved.Publisher);
            Assert.True(saved.IsApplied);
            Assert.True(saved.UpdatedAt.ToUniversalTime() > SeedUpdatedAt);

            // Each composition keeps its own antibiotic combination
            Assert.Equal(2, saved.Treatments.Count);
            var compositions = saved.Treatments
                .Select(x => CompositionKey(x.Combination.Select(a => a.Id)))
                .ToList();
            Assert.Contains(CompositionKey([meropenem.Id]), compositions);
            Assert.Contains(CompositionKey([azithromycin.Id]), compositions);
            Assert.All(saved.Treatments, x => Assert.Equal(saved.Id, x.TreatmentProtocolId));
            Assert.Equal(2, await CountTreatmentsAsync());

            // Business rule: the pathogen and criteria lists are persisted as AND-filters
            // and replace the previously linked rows
            Assert.Equal(
                new[] { pseudomonas.Id, mycoplasma.Id }.Order(),
                saved.Pathogens.Select(x => x.Id).Order());
            Assert.Equal(
                new[] { noPenicillinAllergy.Id, renalImpairment.Id }.Order(),
                saved.Criteria.Select(x => x.Id).Order());
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_DifferentLineageCombination_Success()
        {
            /*
             * Business rule from IsMedicineCombinationValid: a composition may contain
             * multiple antibiotics as long as they do not share a group ancestor.
             * Ceftriaxone (cephalosporin) and azithromycin (macrolide) come from two
             * unrelated lineages, so the update must be accepted
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var ceftriaxone = Antibiotic(
                "Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var oldPathogen = Pathogen("Streptococcus pneumoniae", isAtypical: false);

            await SeedAsync(betaLactams);
            var protocol = await SeedProtocolAsync(
                cephalosporins, ceftriaxone, oldPathogen);

            var macrolides = Group("Macrolides");
            var azithromycin = Antibiotic(
                "Azithromycin", macrolides.Id, AwareClassification.Watch);
            await SeedAsync(macrolides, azithromycin);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Moderate,
                    TreatmentSite = TreatmentSite.Outpatient,
                    Medicines = [[ceftriaxone.Id, azithromycin.Id]],
                    Pathogens = [],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var saved = await LoadProtocolAsync(protocol.Id);
            var treatment = Assert.Single(saved.Treatments);
            Assert.Equal(
                new[] { ceftriaxone.Id, azithromycin.Id }.Order(),
                treatment.Combination.Select(x => x.Id).Order());
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_DuplicateAntibioticAcrossCompositions_Success()
        {
            /*
             * The same antibiotic can appear in more than one alternative composition.
             * The handler de-duplicates antibiotic IDs before counting them in the
             * database, so sharing an antibiotic must not be mistaken for a missing one
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var ceftriaxone = Antibiotic(
                "Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);

            await SeedAsync(betaLactams);
            var protocol = await SeedProtocolAsync(cephalosporins, ceftriaxone);

            var macrolides = Group("Macrolides");
            var azithromycin = Antibiotic(
                "Azithromycin", macrolides.Id, AwareClassification.Watch);
            await SeedAsync(macrolides, azithromycin);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Moderate,
                    TreatmentSite = TreatmentSite.Inpatient,
                    Medicines =
                    [
                        [ceftriaxone.Id],
                        [ceftriaxone.Id, azithromycin.Id],
                    ],
                    Pathogens = [],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = false,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var saved = await LoadProtocolAsync(protocol.Id);
            Assert.Equal(2, saved.Treatments.Count);
            Assert.Contains(saved.Treatments, x =>
                CompositionKey(x.Combination.Select(a => a.Id)) == CompositionKey([ceftriaxone.Id]));
            Assert.Contains(saved.Treatments, x =>
                CompositionKey(x.Combination.Select(a => a.Id))
                    == CompositionKey([ceftriaxone.Id, azithromycin.Id]));
            Assert.Equal(2, await CountTreatmentsAsync());
        }

        #endregion

        #region Fail path

        /*
         * Note: the mapper failure branch cannot be tested because
         * UpdateTreatmentProtocolMapper always returns a successful result.
         *
         * The combination validation failure branch (validateResult.IsFailure) is also
         * unreachable through the application: it requires an active antibiotic whose
         * group is missing from the clinical context, but deleting a group cascades the
         * soft delete to all its antibiotics and creating/updating an antibiotic only
         * accepts an active group.
         *
         * Empty Medicines is rejected by UpdateTreatmentProtocolValidator before the
         * command reaches the handler, so it is covered by the validator test instead.
         */

        [Fact]
        public async Task UpdateTreatmentProtocol_ProtocolNotFound_Fail()
        {
            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = Guid.CreateVersion7(),
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                    Medicines = [[Guid.CreateVersion7()]],
                    Pathogens = [],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_SoftDeletedProtocol_Fail()
        {
            // A soft-deleted protocol is hidden by the query filter, so updating it must
            // be rejected just like an unknown protocol
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic(
                "Amoxicillin", group.Id, AwareClassification.Access);
            var protocol = await SeedProtocolAsync(
                group, amoxicillin, softDeleted: true);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                    Medicines = [[amoxicillin.Id]],
                    Pathogens = [],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertProtocolNotModifiedAsync(protocol);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_UnknownAntibiotic_Fail()
        {
            // One existing antibiotic and one ID that does not exist: the update must be
            // rejected as a whole, leaving the old protocol intact
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic(
                "Amoxicillin", group.Id, AwareClassification.Access);
            var protocol = await SeedProtocolAsync(group, amoxicillin);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.Inpatient,
                    Medicines = [[amoxicillin.Id, Guid.CreateVersion7()]],
                    Pathogens = [],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertProtocolNotModifiedAsync(protocol);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_SoftDeletedAntibiotic_Fail()
        {
            // A soft-deleted antibiotic is hidden by the query filter, so referencing
            // it must be rejected just like an unknown antibiotic
            var group = Group("Carbapenems");
            var meropenem = Antibiotic(
                "Meropenem", group.Id, AwareClassification.Watch, softDeleted: true);
            var protocol = await SeedProtocolAsync(group, meropenem);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                    Medicines = [[meropenem.Id]],
                    Pathogens = [],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertProtocolNotModifiedAsync(protocol);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_UnknownPathogen_Fail()
        {
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic(
                "Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var protocol = await SeedProtocolAsync(
                group, amoxicillin, pneumococcus);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                    Medicines = [[amoxicillin.Id]],
                    Pathogens = [pneumococcus.Id, Guid.CreateVersion7()],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertProtocolNotModifiedAsync(protocol);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_SoftDeletedPathogen_Fail()
        {
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic(
                "Amoxicillin", group.Id, AwareClassification.Access);
            var deletedPathogen = Pathogen(
                "Legionella pneumophila", isAtypical: true, softDeleted: true);
            var protocol = await SeedProtocolAsync(group, amoxicillin);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Moderate,
                    TreatmentSite = TreatmentSite.Inpatient,
                    Medicines = [[amoxicillin.Id]],
                    Pathogens = [deletedPathogen.Id],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertProtocolNotModifiedAsync(protocol);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_UnknownCriterion_Fail()
        {
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic(
                "Amoxicillin", group.Id, AwareClassification.Access);
            var adult = Criterion("Patient is an adult");
            var protocol = await SeedProtocolAsync(
                group, amoxicillin, criterion: adult);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                    Medicines = [[amoxicillin.Id]],
                    Pathogens = [],
                    Criteria = [adult.Id, Guid.CreateVersion7()],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertProtocolNotModifiedAsync(protocol);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_SoftDeletedCriterion_Fail()
        {
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic(
                "Amoxicillin", group.Id, AwareClassification.Access);
            var deletedCriterion = Criterion(
                "No penicillin allergy", softDeleted: true);
            var protocol = await SeedProtocolAsync(group, amoxicillin);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.Outpatient,
                    Medicines = [[amoxicillin.Id]],
                    Pathogens = [],
                    Criteria = [deletedCriterion.Id],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            await AssertProtocolNotModifiedAsync(protocol);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_CombinationWithCommonAncestor_Fail()
        {
            /*
             * Business rule from AreAntibioticGroupsHaveCommonAncestor: ceftriaxone
             * (cephalosporin) and meropenem (carbapenem) both descend from the
             * beta-lactam group, so they cannot be combined in a single treatment.
             * The handler must report a business rule violation and persist nothing,
             * including the scalar fields it already mapped
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var ceftriaxone = Antibiotic(
                "Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            await SeedAsync(betaLactams);
            var protocol = await SeedProtocolAsync(cephalosporins, ceftriaxone);

            var carbapenems = Group("Carbapenems", betaLactams.Id);
            var meropenem = Antibiotic(
                "Meropenem", carbapenems.Id, AwareClassification.Watch);
            await SeedAsync(carbapenems, meropenem);

            var result = await _handler.HandleAsync(
                new UpdateTreatmentProtocolCommand
                {
                    Id = protocol.Id,
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                    Medicines = [[ceftriaxone.Id, meropenem.Id]],
                    Pathogens = [],
                    Criteria = [],
                    Version = "2026.1",
                    Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                    IsApplied = true,
                }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            await AssertProtocolNotModifiedAsync(protocol);
        }

        #endregion
    }
}
