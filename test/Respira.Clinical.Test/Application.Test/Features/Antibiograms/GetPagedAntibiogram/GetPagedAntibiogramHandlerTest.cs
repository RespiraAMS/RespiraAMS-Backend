using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Antibiograms.GetPagedAntibiogram;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.GetPagedAntibiogram
{
    public class GetPagedAntibiogramHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetPagedAntibiogramHandler _handler;
        private readonly IDbContext _context;

        public GetPagedAntibiogramHandlerTest(PostgresFixture fixture)
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
            // soft-deleted rows are hidden by the query filter but still occupy the table
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
        }

        #region Seed helpers

        /*
         * Reference data is written through its own context and each layer is saved
         * before the handler runs, so the handler context starts clean just like a real
         * HTTP request.
         *
         * The five live antibiograms model susceptibility panels for community and
         * hospital acquired pneumonia, and their CreatedAt values are spaced 1 minute
         * apart so that the CreatedAt-descending order of the handler is deterministic.
         *
         * Newest first: pneumococcus susceptible, pneumococcus intermediate,
         *               pneumococcus resistant, mycoplasma susceptible,
         *               pseudomonas resistant
         *
         * The sixth antibiogram is soft deleted and must never show up in any result.
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
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            var pneumococcusSusceptible = AntibiogramEntity(
                pneumococcus.Id, MicCategory.Susceptible, baseTime, [amoxicillin]);
            var pneumococcusIntermediate = AntibiogramEntity(
                pneumococcus.Id, MicCategory.Intermediate, baseTime.AddMinutes(-1),
                [ceftriaxone, azithromycin]);
            var pneumococcusResistant = AntibiogramEntity(
                pneumococcus.Id, MicCategory.Resistant, baseTime.AddMinutes(-2),
                [meropenem]);
            var mycoplasmaSusceptible = AntibiogramEntity(
                mycoplasma.Id, MicCategory.Susceptible, baseTime.AddMinutes(-3),
                [azithromycin]);
            var pseudomonasResistant = AntibiogramEntity(
                pseudomonas.Id, MicCategory.Resistant, baseTime.AddMinutes(-4),
                [meropenem, levofloxacin]);
            var softDeleted = AntibiogramEntity(
                pseudomonas.Id, MicCategory.Susceptible, baseTime.AddMinutes(-5),
                [amoxicillin], softDeleted: true);

            seedContext.AddRange(
                pneumococcusSusceptible, pneumococcusIntermediate, pneumococcusResistant,
                mycoplasmaSusceptible, pseudomonasResistant, softDeleted);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new SeedResult
            {
                PneumococcusSusceptible = pneumococcusSusceptible,
                PneumococcusIntermediate = pneumococcusIntermediate,
                PneumococcusResistant = pneumococcusResistant,
                MycoplasmaSusceptible = mycoplasmaSusceptible,
                PseudomonasResistant = pseudomonasResistant,
                SoftDeleted = softDeleted,
                Live =
                [
                    pneumococcusSusceptible, pneumococcusIntermediate, pneumococcusResistant,
                    mycoplasmaSusceptible, pseudomonasResistant,
                ],
                Pneumococcus = pneumococcus,
                Mycoplasma = mycoplasma,
                Ceftriaxone = ceftriaxone,
                Azithromycin = azithromycin,
                Cephalosporins = cephalosporins,
                Macrolides = macrolides,
            };
        }

        private sealed class SeedResult
        {
            public required Antibiogram PneumococcusSusceptible { get; init; }
            public required Antibiogram PneumococcusIntermediate { get; init; }
            public required Antibiogram PneumococcusResistant { get; init; }
            public required Antibiogram MycoplasmaSusceptible { get; init; }
            public required Antibiogram PseudomonasResistant { get; init; }
            public required Antibiogram SoftDeleted { get; init; }
            public required Antibiogram[] Live { get; init; }
            public required Pathogen Pneumococcus { get; init; }
            public required Pathogen Mycoplasma { get; init; }
            public required Antibiotic Ceftriaxone { get; init; }
            public required Antibiotic Azithromycin { get; init; }
            public required AntibioticGroup Cephalosporins { get; init; }
            public required AntibioticGroup Macrolides { get; init; }
        }

        private static Antibiogram AntibiogramEntity(
            Guid pathogenId,
            MicCategory micCategory,
            DateTimeOffset createdAt,
            Antibiotic[] antibiotics,
            bool softDeleted = false)
        {
            return new Antibiogram
            {
                PathogenId = pathogenId,
                MicCategory = micCategory,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? createdAt.AddMinutes(1) : null,
                Antibiotics = [.. antibiotics],
            };
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

        private static GetPagedAntibiogramQuery Query(
            int page = 1, int size = 10, AntibiogramFilter? filter = null)
        {
            return new GetPagedAntibiogramQuery
            {
                Param = new PaginationParam { Page = page, Size = size },
                Filter = filter,
            };
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task GetPagedAntibiogram_NoFilter_FirstPageNewestFirst_Success()
        {
            // 5 items with page size 2 -> pages of [2, 2, 1]
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 2), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // Business rule: the browse endpoint is ordered by CreatedAt descending
            Assert.Equal(
            [
                seed.PneumococcusSusceptible.Id,
                seed.PneumococcusIntermediate.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);

            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedAntibiogram_MiddlePage_HasBothNeighbors_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 2, size: 2), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.Equal(
            [
                seed.PneumococcusResistant.Id,
                seed.MycoplasmaSusceptible.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);

            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
            Assert.Equal(2, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedAntibiogram_LastPartialPage_HasNoNext_Success()
        {
            // Upper boundary page: only 1 leftover item and no next page
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 3, size: 2), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seed.PseudomonasResistant.Id, item.Id);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task GetPagedAntibiogram_PageBeyondRange_ReturnsNoItems()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 4, size: 2), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        /*
         * Boundary values of the pagination: size 1 is the smallest allowed page size
         * and size 100 the largest, so the same five antibiograms must come back either
         * one at a time or all together
         */
        [Fact]
        public async Task GetPagedAntibiogram_MinimumPageSize_WalksOneItemPerPage_Success()
        {
            var seed = await SeedAsync();

            var first = await _handler.HandleAsync(
                Query(page: 1, size: 1), TestContext.Current.CancellationToken);
            Assert.True(first.IsSuccess());
            Assert.NotNull(first.Data);
            Assert.Equal(seed.PneumococcusSusceptible.Id, Assert.Single(first.Data.Items).Id);
            Assert.True(first.Data.Metadata.HasNextPage);
            Assert.False(first.Data.Metadata.HasPreviousPage);

            // The oldest live antibiogram sits on the 5th and last page
            var last = await _handler.HandleAsync(
                Query(page: 5, size: 1), TestContext.Current.CancellationToken);
            Assert.True(last.IsSuccess());
            Assert.NotNull(last.Data);
            Assert.Equal(seed.PseudomonasResistant.Id, Assert.Single(last.Data.Items).Id);
            Assert.True(last.Data.Metadata.HasPreviousPage);
            Assert.False(last.Data.Metadata.HasNextPage);

            var beyond = await _handler.HandleAsync(
                Query(page: 6, size: 1), TestContext.Current.CancellationToken);
            Assert.True(beyond.IsSuccess());
            Assert.NotNull(beyond.Data);
            Assert.Empty(beyond.Data.Items);
        }

        [Fact]
        public async Task GetPagedAntibiogram_MaxPageSize_AllItemsFitOnFirstPage_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 100), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Items.Count());
            Assert.Equal(1, result.Data.Metadata.PageCount);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task GetPagedAntibiogram_EmptyFilterObject_ReturnsAll_Success()
        {
            // A non-null filter with every member unset is equivalent to no filter at
            // all, which is the shape a client sends when it just opens the browse screen
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(filter: new AntibiogramFilter()),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(
                seed.Live.Select(x => x.Id),
                result.Data.Items.Select(x => x.Id));
        }

        [Fact]
        public async Task GetPagedAntibiogram_EmptyDatabase_ReturnsEmptySuccess()
        {
            var result = await _handler.HandleAsync(
                Query(page: 1, size: 10), TestContext.Current.CancellationToken);

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

        /*=== filter ===*/

        [Fact]
        public async Task GetPagedAntibiogram_PathogenFilter_ReturnsOnlyThatPathogen_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 100, filter: new AntibiogramFilter
                {
                    PathogenId = seed.Pneumococcus.Id,
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            // Business rule: the pathogen filter keeps only that pathogen's panels
            Assert.Equal(
            [
                seed.PneumococcusSusceptible.Id,
                seed.PneumococcusIntermediate.Id,
                seed.PneumococcusResistant.Id,
            ], [.. result.Data.Items.Select(x => x.Id)]);
            Assert.All(result.Data.Items,
                x => Assert.Equal(seed.Pneumococcus.Id, x.Pathogen.Id));
            Assert.Equal(3, result.Data.Metadata.TotalItemCount);
        }

        // Every defined MicCategory member is a valid filter
        [Theory]
        [InlineData(MicCategory.Susceptible)]
        [InlineData(MicCategory.Intermediate)]
        [InlineData(MicCategory.Resistant)]
        public async Task GetPagedAntibiogram_MicCategoryFilter_ReturnsOnlyThatCategory_Success(
            MicCategory micCategory)
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 100, filter: new AntibiogramFilter
                {
                    MicCategory = micCategory,
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.All(result.Data.Items, x => Assert.Equal(micCategory, x.MicCategory));

            var expected = seed.Live
                .Where(x => x.MicCategory == micCategory)
                .Select(x => x.Id)
                .ToList();
            Assert.Equal(expected.Count, result.Data.Metadata.TotalItemCount);
            Assert.Equal(expected, [.. result.Data.Items.Select(x => x.Id)]);
        }

        [Fact]
        public async Task GetPagedAntibiogram_CombinedFilters_AppliedTogether_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 100, filter: new AntibiogramFilter
                {
                    PathogenId = seed.Pneumococcus.Id,
                    MicCategory = MicCategory.Resistant,
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            // Only the resistant pneumococcus panel satisfies both filters
            var item = Assert.Single(result.Data.Items);
            Assert.Equal(seed.PneumococcusResistant.Id, item.Id);
            Assert.Equal(seed.Pneumococcus.Id, item.Pathogen.Id);
            Assert.Equal(MicCategory.Resistant, item.MicCategory);
        }

        [Fact]
        public async Task GetPagedAntibiogram_UnknownPathogenId_ReturnsEmpty()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 100, filter: new AntibiogramFilter
                {
                    PathogenId = Guid.CreateVersion7(),
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        [Fact]
        public async Task GetPagedAntibiogram_FilterMatchesNothing_ReturnsEmpty()
        {
            var seed = await SeedAsync();

            // Mycoplasma only has a susceptible panel, so the intersection is empty
            var result = await _handler.HandleAsync(
                Query(page: 1, size: 100, filter: new AntibiogramFilter
                {
                    PathogenId = seed.Mycoplasma.Id,
                    MicCategory = MicCategory.Resistant,
                }),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.Equal(0, result.Data.Metadata.PageCount);
        }

        /*=== projection ===*/

        [Fact]
        public async Task GetPagedAntibiogram_ProjectsPathogenAndAntibiotics_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 100), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items,
                x => x.Id == seed.PneumococcusIntermediate.Id);

            // Business rule: each item carries the tested pathogen and the MIC category
            Assert.Equal(seed.Pneumococcus.Id, item.Pathogen.Id);
            Assert.Equal("Streptococcus pneumoniae", item.Pathogen.Name);
            Assert.Equal(MicCategory.Intermediate, item.MicCategory);

            // Business rule: each antibiotic result carries its identity, name, group and
            // AWaRe classification
            Assert.Equal(2, item.Antibiotics.Count());

            var ceftriaxone = Assert.Single(item.Antibiotics,
                x => x.Id == seed.Ceftriaxone.Id);
            Assert.Equal("Ceftriaxone", ceftriaxone.Name);
            Assert.Equal(seed.Cephalosporins.Id, ceftriaxone.AntibioticGroupId);
            Assert.Equal("Cephalosporins", ceftriaxone.AntibioticGroupName);
            Assert.Equal(AwareClassification.Watch, ceftriaxone.Classification);

            var azithromycin = Assert.Single(item.Antibiotics,
                x => x.Id == seed.Azithromycin.Id);
            Assert.Equal("Azithromycin", azithromycin.Name);
            Assert.Equal(seed.Macrolides.Id, azithromycin.AntibioticGroupId);
            Assert.Equal("Macrolides", azithromycin.AntibioticGroupName);
            Assert.Equal(AwareClassification.Watch, azithromycin.Classification);
        }

        /*=== soft delete ===*/

        [Fact]
        public async Task GetPagedAntibiogram_ExcludesSoftDeleted_Success()
        {
            var seed = await SeedAsync();

            var result = await _handler.HandleAsync(
                Query(page: 1, size: 100), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.DoesNotContain(result.Data.Items, x => x.Id == seed.SoftDeleted.Id);
            Assert.Equal(5, result.Data.Items.Select(x => x.Id).Distinct().Count());
        }

        #endregion

        #region Fail path

        /*
         * GetPagedAntibiogramHandler has no failure branch: the pagination parameters are
         * validated by GetPagedAntibiogramValidator before the handler runs, the
         * projection cannot fail, and the operation is read only. The only failure a
         * database outage would produce is not a behaviour of this feature.
         */

        #endregion
    }
}
