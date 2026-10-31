using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.TreatmentProtocols.GetPagedTreatmentProtocol
{
    public class GetPagedTreatmentProtocolHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetPagedTreatmentProtocolHandler _handler;
        private readonly IDbContext _context;

        public GetPagedTreatmentProtocolHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            IPaginationFactory factory = new PaginationFactory();

            // Initialize handler
            _handler = new(_context, factory);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the ordering and
            // count assertions are deterministic across runs. IgnoreQueryFilters because
            // soft deleted rows are hidden by the query filter but still occupy the table
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
         * Reference data is written through its own context and each layer is saved before
         * the handler runs, so the handler context starts clean just like a real HTTP
         * request. The five live protocols model the clinical pathways of community and
         * hospital acquired pneumonia, and their CreatedAt values are spaced 1 minute
         * apart so that the CreatedAt-descending order of the handler is deterministic.
         *
         * Newest first: CAP mild outpatient, CAP moderate inpatient, CAP severe ICU,
         *               atypical pneumonia outpatient, HAP severe inpatient
         *
         * The sixth protocol is soft deleted and must never show up in any result.
         */
        private async Task<SeedResult> SeedAsync()
        {
            var baseTime = DateTimeOffset.UtcNow;

            var betaLactams = Group("Beta-lactams", parentId: null);
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var carbapenems = Group("Carbapenems", betaLactams.Id);
            var aminopenicillins = Group("Aminopenicillins", betaLactams.Id);
            var macrolides = Group("Macrolides", parentId: null);
            var fluoroquinolones = Group("Fluoroquinolones", parentId: null);

            var pneumococcus = PathogenEntity("Streptococcus pneumoniae", isAtypical: false);
            var mycoplasma = PathogenEntity("Mycoplasma pneumoniae", isAtypical: true);
            var pseudomonas = PathogenEntity("Pseudomonas aeruginosa", isAtypical: false);

            // Real CURB-65 style thresholds: adult age and fever over 38 degrees Celsius
            var adult = CriterionEntity(
                "Patient is an adult", Numeric("AGE", 18, ExpressionOperator.Gte));
            var fever = CriterionEntity(
                "Fever over 38", Numeric("TEMP", 38, ExpressionOperator.Gt));

            await using var seedContext = new ClinicalDbContext(_options);

            // Root groups first, then their sub-groups, so the self-referencing FK on
            // antibiotic_groups is always satisfied
            seedContext.AddRange(betaLactams, macrolides, fluoroquinolones);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            seedContext.AddRange(cephalosporins, carbapenems, aminopenicillins);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var ceftriaxone = AntibioticEntity(
                "Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var meropenem = AntibioticEntity(
                "Meropenem", carbapenems.Id, AwareClassification.Watch);
            var amoxicillin = AntibioticEntity(
                "Amoxicillin", aminopenicillins.Id, AwareClassification.Access);
            var azithromycin = AntibioticEntity(
                "Azithromycin", macrolides.Id, AwareClassification.Watch);
            var levofloxacin = AntibioticEntity(
                "Levofloxacin", fluoroquinolones.Id, AwareClassification.Watch);

            seedContext.AddRange(
                ceftriaxone, meropenem, amoxicillin, azithromycin, levofloxacin);
            seedContext.AddRange(pneumococcus, mycoplasma, pseudomonas);
            seedContext.AddRange(adult, fever);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var capMildOutpatient = Protocol(
                Severity.Mild, TreatmentSite.Outpatient, baseTime,
                medicines: [[amoxicillin]],
                pathogens: [pneumococcus],
                criteria: [adult]);
            var capModerateInpatient = Protocol(
                Severity.Moderate, TreatmentSite.Inpatient, baseTime.AddMinutes(-1),
                medicines: [[ceftriaxone]],
                pathogens: [pneumococcus, mycoplasma],
                criteria: [adult, fever]);
            var capSevereIcu = Protocol(
                Severity.Severe, TreatmentSite.IntensiveCareUnit, baseTime.AddMinutes(-2),
                medicines: [[ceftriaxone, azithromycin], [meropenem]],
                pathogens: [pneumococcus, pseudomonas],
                criteria: [adult]);
            var atypicalOutpatient = Protocol(
                Severity.Mild, TreatmentSite.Outpatient, baseTime.AddMinutes(-3),
                medicines: [[azithromycin]],
                pathogens: [mycoplasma],
                criteria: [adult]);
            var hapSevereInpatient = Protocol(
                Severity.Severe, TreatmentSite.Inpatient, baseTime.AddMinutes(-4),
                medicines: [[levofloxacin]],
                pathogens: [pseudomonas],
                criteria: [fever]);
            var softDeleted = Protocol(
                Severity.Severe, TreatmentSite.IntensiveCareUnit, baseTime.AddMinutes(-5),
                medicines: [[meropenem]],
                pathogens: [pseudomonas],
                criteria: [fever],
                softDeleted: true);

            seedContext.AddRange(
                capMildOutpatient, capModerateInpatient, capSevereIcu,
                atypicalOutpatient, hapSevereInpatient, softDeleted);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new SeedResult
            {
                CapMildOutpatient = capMildOutpatient,
                CapModerateInpatient = capModerateInpatient,
                CapSevereIcu = capSevereIcu,
                AtypicalOutpatient = atypicalOutpatient,
                HapSevereInpatient = hapSevereInpatient,
                SoftDeleted = softDeleted,
                Pneumococcus = pneumococcus,
                Mycoplasma = mycoplasma,
                Pseudomonas = pseudomonas,
                Adult = adult,
                Fever = fever,
                Ceftriaxone = ceftriaxone,
                Meropenem = meropenem,
                Azithromycin = azithromycin,
                Cephalosporins = cephalosporins,
                Carbapenems = carbapenems,
                Macrolides = macrolides,
            };
        }

        private sealed class SeedResult
        {
            public required TreatmentProtocol CapMildOutpatient { get; init; }
            public required TreatmentProtocol CapModerateInpatient { get; init; }
            public required TreatmentProtocol CapSevereIcu { get; init; }
            public required TreatmentProtocol AtypicalOutpatient { get; init; }
            public required TreatmentProtocol HapSevereInpatient { get; init; }
            public required TreatmentProtocol SoftDeleted { get; init; }
            public required Pathogen Pneumococcus { get; init; }
            public required Pathogen Mycoplasma { get; init; }
            public required Pathogen Pseudomonas { get; init; }
            public required Criterion Adult { get; init; }
            public required Criterion Fever { get; init; }
            public required Antibiotic Ceftriaxone { get; init; }
            public required Antibiotic Meropenem { get; init; }
            public required Antibiotic Azithromycin { get; init; }
            public required AntibioticGroup Cephalosporins { get; init; }
            public required AntibioticGroup Carbapenems { get; init; }
            public required AntibioticGroup Macrolides { get; init; }
        }

        private static TreatmentProtocol Protocol(
            Severity severity,
            TreatmentSite treatmentSite,
            DateTimeOffset createdAt,
            Antibiotic[][] medicines,
            Pathogen[] pathogens,
            Criterion[] criteria,
            bool softDeleted = false)
        {
            var protocol = new TreatmentProtocol
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                Version = "2026.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? createdAt.AddMinutes(1) : null,
            };

            protocol.Treatments = [.. medicines
                .Select(x => new Treatment
                {
                    TreatmentProtocolId = protocol.Id,
                    Combination = x,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt,
                    IsDeleted = softDeleted,
                    DeletedAt = softDeleted ? createdAt.AddMinutes(1) : null,
                })];
            protocol.Pathogens = pathogens;
            protocol.Criteria = criteria;

            return protocol;
        }

        private static AntibioticGroup Group(string name, Guid? parentId)
        {
            return new AntibioticGroup
            {
                Name = name,
                Description = $"{name} antibiotic group",
                ParentId = parentId,
            };
        }

        private static Antibiotic AntibioticEntity(
            string name, Guid antibioticGroupId, AwareClassification classification)
        {
            return new Antibiotic
            {
                Name = name,
                AntibioticGroupId = antibioticGroupId,
                Classification = classification,
            };
        }

        private static Pathogen PathogenEntity(string name, bool isAtypical)
        {
            return new Pathogen
            {
                Name = name,
                Description = $"{name} pathogen",
                IsAtypical = isAtypical,
            };
        }

        private static Criterion CriterionEntity(string name, Formula formula)
        {
            return new Criterion
            {
                Name = name,
                Formula = formula,
            };
        }

        private static Formula Numeric(
            string code, decimal threshold, ExpressionOperator expressionOperator)
        {
            return new BinaryFormula(
                new VariableFormula(
                    new VariableRef(Guid.CreateVersion7(), code, ClinicalValueType.Numeric)),
                new NumericConstantFormula(threshold),
                expressionOperator);
        }

        private static GetPagedTreatmentProtocolQuery Query(
            int page = 1, int size = 10, TreatmentProtocolFilter? filter = null)
        {
            return new GetPagedTreatmentProtocolQuery
            {
                Param = new PaginationParam { Page = page, Size = size },
                Filter = filter,
            };
        }

        private static string CompositionKey(IEnumerable<Guid> antibioticIds)
        {
            return string.Join('|', antibioticIds.Order());
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task GetPagedTreatmentProtocol_FirstPageNewestFirst_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 2),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Business rule: the browse endpoint is ordered by CreatedAt descending
            Assert.Equal(
            [
                seed.CapMildOutpatient.Id,
                seed.CapModerateInpatient.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_EmptyFilterObject_ReturnsAll_Success()
        {
            // A non-null filter with every member unset is equivalent to no filter at all,
            // which is the shape a client sends when it just opens the browse screen
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(
            [
                seed.CapMildOutpatient.Id,
                seed.CapModerateInpatient.Id,
                seed.CapSevereIcu.Id,
                seed.AtypicalOutpatient.Id,
                seed.HapSevereInpatient.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_LastPartialPage_HasNoNext_Success()
        {
            // Upper boundary page: only 1 leftover item and no next page
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 3, size: 2),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seed.HapSevereInpatient.Id, item.Id);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_PageBeyondRange_ReturnsNoItems()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 4, size: 2),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        /*
         * Boundary values of the pagination: size 1 is the smallest allowed page size and
         * size 100 the largest, so the same five protocols must come back either one at a
         * time or all together
         */
        [Fact]
        public async Task GetPagedTreatmentProtocol_MinimumPageSize_WalksOneItemPerPage_Success()
        {
            var seed = await SeedAsync();

            var first = await _handler.HandleAsync(Query(page: 1, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());
            Assert.NotNull(first.Data);
            Assert.Equal(seed.CapMildOutpatient.Id, Assert.Single(first.Data.Items).Id);
            Assert.True(first.Data.Metadata.HasNextPage);
            Assert.False(first.Data.Metadata.HasPreviousPage);

            // The oldest live protocol sits on the 5th and last page
            var last = await _handler.HandleAsync(Query(page: 5, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(last.IsSuccess());
            Assert.NotNull(last.Data);
            Assert.Equal(seed.HapSevereInpatient.Id, Assert.Single(last.Data.Items).Id);
            Assert.True(last.Data.Metadata.HasPreviousPage);
            Assert.False(last.Data.Metadata.HasNextPage);

            var beyond = await _handler.HandleAsync(Query(page: 6, size: 1),
                TestContext.Current.CancellationToken);
            Assert.True(beyond.IsSuccess());
            Assert.NotNull(beyond.Data);
            Assert.Empty(beyond.Data.Items);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_MaxPageSize_AllItemsFitOnFirstPage_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Items.Count());
            Assert.Equal(1, result.Data.Metadata.PageCount);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        /*=== filter ===*/

        [Fact]
        public async Task GetPagedTreatmentProtocol_SeverityFilter_ReturnsOnlyMatchingOnes_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter { Severity = Severity.Mild }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(
            [
                seed.CapMildOutpatient.Id,
                seed.AtypicalOutpatient.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);
            Assert.All(result.Data.Items, x => Assert.Equal(Severity.Mild, x.Severity));
            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_TreatmentSiteFilter_ReturnsOnlyMatchingOnes_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter
                {
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seed.CapSevereIcu.Id, item.Id);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, item.TreatmentSite);
            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_PathogenFilter_ReturnsProtocolsContainingIt_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter
                {
                    PathogenIds = [seed.Mycoplasma.Id],
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Both the moderate CAP protocol (pneumococcus + mycoplasma) and the atypical
            // pneumonia protocol (mycoplasma only) qualify
            Assert.Equal(
            [
                seed.CapModerateInpatient.Id,
                seed.AtypicalOutpatient.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);
            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_CriteriaFilter_ReturnsProtocolsHavingIt_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter
                {
                    CriteriaIds = [seed.Fever.Id],
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(
            [
                seed.CapModerateInpatient.Id,
                seed.HapSevereInpatient.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);
            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_CombinedFilters_AppliedTogether_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter
                {
                    Severity = Severity.Severe,
                    TreatmentSite = TreatmentSite.Inpatient,
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Only HAP severe inpatient satisfies both filters: the severe CAP protocol is
            // treated in the ICU and the mild CAP protocol is treated as outpatient
            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seed.HapSevereInpatient.Id, item.Id);
            Assert.Equal(Severity.Severe, item.Severity);
            Assert.Equal(TreatmentSite.Inpatient, item.TreatmentSite);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_SeverityAndPathogenFilters_AppliedTogether_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter
                {
                    Severity = Severity.Severe,
                    PathogenIds = [seed.Pseudomonas.Id],
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(
            [
                seed.CapSevereIcu.Id,
                seed.HapSevereInpatient.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);
        }

        /*
         * The id list filters use OR semantics: a protocol is returned when it is linked
         * to ANY of the requested ids, so an unknown id in the list must not discard the
         * protocols that do match the known ids
         */
        [Fact]
        public async Task GetPagedTreatmentProtocol_PathogenFilterWithUnknownId_StillMatchesKnownIds_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter
                {
                    PathogenIds = [Guid.CreateVersion7(), seed.Mycoplasma.Id],
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(
            [
                seed.CapModerateInpatient.Id,
                seed.AtypicalOutpatient.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_CriteriaFilterWithUnknownId_StillMatchesKnownIds_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter
                {
                    CriteriaIds = [Guid.CreateVersion7(), seed.Fever.Id],
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(
            [
                seed.CapModerateInpatient.Id,
                seed.HapSevereInpatient.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_FilterMatchesNothing_ReturnsEmpty()
        {
            await SeedAsync();

            // Mild severity is never treated in the ICU, so the intersection is empty
            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter
                {
                    Severity = Severity.Mild,
                    TreatmentSite = TreatmentSite.IntensiveCareUnit,
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.Equal(0, result.Data.Metadata.PageCount);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_EmptyIdFilterList_ReturnsEmpty()
        {
            /*
             * An omitted id list (null) means "do not filter", while an explicitly empty
             * list means "match a protocol linked to any of no pathogens", so it can never
             * match. This documents the DTO contract where null and empty differ
             */
            await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new TreatmentProtocolFilter { PathogenIds = [] }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        /*=== projection ===*/

        [Fact]
        public async Task GetPagedTreatmentProtocol_ProjectsTreatmentsWithAntibioticGroups_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items, x => x.Id == seed.CapSevereIcu.Id);

            // The severe CAP protocol has 2 alternative compositions: a two-antibiotic
            // combination and a single antibiotic
            Assert.Equal(2, item.Treatments.Count());
            var compositions = item.Treatments
                .Select(x => CompositionKey(x.Select(a => a.Id)))
                .ToList();
            Assert.Contains(CompositionKey([seed.Ceftriaxone.Id, seed.Azithromycin.Id]), compositions);
            Assert.Contains(CompositionKey([seed.Meropenem.Id]), compositions);

            // Business rule: each antibiotic result carries its identity, its name, the
            // group it belongs to and the AWaRe classification
            var combination = Assert.Single(item.Treatments, x => x.Count() == 2);

            var ceftriaxone = Assert.Single(combination, x => x.Id == seed.Ceftriaxone.Id);
            Assert.Equal("Ceftriaxone", ceftriaxone.Name);
            Assert.Equal(seed.Cephalosporins.Id, ceftriaxone.AntibioticGroupId);
            Assert.Equal("Cephalosporins", ceftriaxone.AntibioticGroupName);
            Assert.Equal(AwareClassification.Watch, ceftriaxone.Classification);

            var azithromycin = Assert.Single(combination, x => x.Id == seed.Azithromycin.Id);
            Assert.Equal("Azithromycin", azithromycin.Name);
            Assert.Equal(seed.Macrolides.Id, azithromycin.AntibioticGroupId);
            Assert.Equal("Macrolides", azithromycin.AntibioticGroupName);
            Assert.Equal(AwareClassification.Watch, azithromycin.Classification);

            var monotherapy = Assert.Single(item.Treatments, x => x.Count() == 1);
            var meropenem = Assert.Single(monotherapy);
            Assert.Equal("Meropenem", meropenem.Name);
            Assert.Equal(seed.Carbapenems.Id, meropenem.AntibioticGroupId);
            Assert.Equal("Carbapenems", meropenem.AntibioticGroupName);
            Assert.Equal(AwareClassification.Watch, meropenem.Classification);
        }

        [Fact]
        public async Task GetPagedTreatmentProtocol_ProjectsPathogensAndRenderedCriteria_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items,
                x => x.Id == seed.CapModerateInpatient.Id);

            // The protocol is projected with its scalars and its AND-linked pathogen list
            Assert.Equal(Severity.Moderate, item.Severity);
            Assert.Equal(TreatmentSite.Inpatient, item.TreatmentSite);
            Assert.Equal(
                new[] { seed.Pneumococcus.Id, seed.Mycoplasma.Id }.Order(),
                item.Pathogens.Select(x => x.Id).Order());
            Assert.Equal(
                ["Mycoplasma pneumoniae", "Streptococcus pneumoniae"],
                item.Pathogens.Select(x => x.Name).Order());

            // Criteria are rendered from the stored JSONB formula, not from the input
            Assert.Equal(
                new[] { seed.Adult.Id, seed.Fever.Id }.Order(),
                item.Criteria.Select(x => x.Id).Order());
            Assert.Equal(
                ["AGE ≥ 18", "TEMP > 38"],
                item.Criteria.Select(x => x.Formula).Order());
            Assert.Equal(
                ["Fever over 38", "Patient is an adult"],
                item.Criteria.Select(x => x.Name).Order());
        }

        /*=== soft delete ===*/

        [Fact]
        public async Task GetPagedTreatmentProtocol_ExcludesSoftDeleted_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(Query(page: 1, size: 100),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == seed.SoftDeleted.Id);
            Assert.Equal(5, result.Data.Items.Select(x => x.Id).Distinct().Count());
        }

        /*=== boundary: no data at all ===*/

        [Fact]
        public async Task GetPagedTreatmentProtocol_EmptyDatabase_ReturnsEmptySuccess()
        {
            var result = await _handler.HandleAsync(Query(page: 1, size: 10),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.Equal(0, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.False(result.Data.Metadata.HasPreviousPage);
        }

        #endregion

        /*
         * Fail path: GetPagedTreatmentProtocolHandler has no failure branch. The pagination
         * is already validated by GetPagedTreatmentProtocolValidator before the handler
         * runs, the projection cannot fail and the operation is read only, so the only
         * failure a database outage would produce is not a behaviour of this feature.
         */
    }
}
