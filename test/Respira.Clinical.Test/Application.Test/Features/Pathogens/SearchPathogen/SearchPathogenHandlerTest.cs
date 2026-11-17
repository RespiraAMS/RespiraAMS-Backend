using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Pathogens.SearchPathogen;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Pathogens.SearchPathogen
{
    public class SearchPathogenHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly SearchPathogenHandler _handler;
        private readonly IDbContext _context;

        public SearchPathogenHandlerTest(PostgresFixture fixture)
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
            // Clear leftover data so the counts are deterministic across runs.
            // IgnoreQueryFilters because soft deleted rows are hidden by the query
            // filter but still occupy the table
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private static Pathogen CreatePathogen(
            string name,
            string description,
            bool isAtypical,
            bool isDeleted = false)
        {
            return new Pathogen
            {
                Name = name,
                Description = description,
                IsAtypical = isAtypical,
                IsDeleted = isDeleted,
                DeletedAt = isDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        /*
         * Seven live pathogens plus one soft deleted pathogen. The five first ones
         * share the phrase "community acquired pneumonia" in their description (weight
         * B) but never in their name, which gives deterministic matching and counts.
         *
         * The last live pathogen uses a Vietnamese common name so accent folding can be
         * exercised.
         */
        private async Task<IReadOnlyList<Pathogen>> SeedMainAsync()
        {
            var seeded = new List<Pathogen>
            {
                CreatePathogen(
                    "Streptococcus pneumoniae",
                    "Lancet shaped gram positive diplococcus, the most common cause of community acquired pneumonia",
                    isAtypical: false),
                CreatePathogen(
                    "Haemophilus influenzae",
                    "Gram negative coccobacillus, a common cause of community acquired pneumonia in patients with COPD",
                    isAtypical: false),
                CreatePathogen(
                    "Mycoplasma pneumoniae",
                    "Atypical pathogen lacking a cell wall, a frequent cause of community acquired pneumonia in young adults",
                    isAtypical: true),
                CreatePathogen(
                    "Legionella pneumophila",
                    "Gram negative bacillus transmitted by aerosols, causes severe community acquired pneumonia",
                    isAtypical: true),
                CreatePathogen(
                    "Chlamydophila pneumoniae",
                    "Obligate intracellular atypical pathogen, a cause of community acquired pneumonia",
                    isAtypical: true),
                CreatePathogen(
                    "Staphylococcus aureus",
                    "Gram positive cocci in clusters, a cause of necrotising pneumonia and empyema",
                    isAtypical: false),
                CreatePathogen(
                    "Trực khuẩn lao",
                    "Vi khuẩn gây bệnh lao, lây truyền qua đường không khí",
                    isAtypical: false),
            };

            await _context.Pathogens.AddRangeAsync(seeded, TestContext.Current.CancellationToken);

            // Soft deleted: hidden by the query filter but still present in the table
            await _context.Pathogens.AddAsync(
                CreatePathogen(
                    "Candida albicans",
                    "Yeast that causes opportunistic infections in immunocompromised patients",
                    isAtypical: false,
                    isDeleted: true),
                TestContext.Current.CancellationToken);

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return seeded;
        }

        #region Happy path

        [Fact]
        public async Task SearchPathogen_PrefixMatchOnName_ReturnsFullProjection_Success()
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "streptococcus",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Streptococcus pneumoniae", item.Name);
            Assert.Equal(
                "Lancet shaped gram positive diplococcus, the most common cause of community acquired pneumonia",
                item.Description);
            Assert.False(item.IsAtypical);
            Assert.True(item.Rank > 0);

            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task SearchPathogen_AtypicalPathogen_ProjectsAtypicalFlag_Success()
        {
            await SeedMainAsync();

            // Mycoplasma pneumoniae has no cell wall, so it is flagged atypical
            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "mycoplasma",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Mycoplasma pneumoniae", item.Name);
            Assert.True(item.IsAtypical);
            Assert.True(item.Rank > 0);
        }

        [Theory]
        [InlineData("streptococcus")] // full word
        [InlineData("Streptococcus")] // the respira_vi configuration lowercases both sides
        [InlineData("strepto")]       // shorter prefix still matches
        public async Task SearchPathogen_PrefixTerm_MatchesSamePathogen_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Streptococcus pneumoniae", item.Name);
        }

        [Fact]
        public async Task SearchPathogen_MultiWordTerm_RequiresEveryWordInDescription_Success()
        {
            await SeedMainAsync();

            // "community acquired" only appears together in the description (weight B)
            // of the five first pathogens, never in a name
            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "community acquired",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var items = result.Data.Items.ToList();
            Assert.Equal(5, items.Count);
            Assert.All(items, x => Assert.True(x.Rank > 0));
            Assert.All(items, x => Assert.DoesNotContain("community", x.Name, StringComparison.OrdinalIgnoreCase));
            Assert.All(items, x => Assert.Contains("community acquired", x.Description, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("truc")]   // unaccented query term
        [InlineData("trực")]   // accented query term
        [InlineData("khuan")]  // unaccented, matches the second word
        [InlineData("khuẩn")]  // accented, matches the second word
        public async Task SearchPathogen_AccentFoldedTerm_MatchesVietnameseData_Success(string searchTerm)
        {
            await SeedMainAsync();

            // Both the stored vector and the query run through the respira_vi
            // configuration, which folds accents with unaccent
            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Trực khuẩn lao", item.Name);
            Assert.False(item.IsAtypical);
        }

        [Fact]
        public async Task SearchPathogen_SameTermInNameAndDescription_RanksNameAboveDescription_Success()
        {
            // A single term placed once in the name of one pathogen (weight A) and once
            // in the description of another (weight B)
            await _context.Pathogens.AddRangeAsync(
            [
                CreatePathogen(
                    "Pseudomonas aeruginosa",
                    "Gram negative rod, an opportunistic nosocomial pathogen",
                    isAtypical: false),
                CreatePathogen(
                    "Burkholderia cepacia complex",
                    "Closely related to the aeruginosa group and intrinsically resistant to many antibiotics",
                    isAtypical: false),
            ], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "aeruginosa",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var ordered = result.Data.Items.ToList();
            Assert.Equal(
            [
                "Pseudomonas aeruginosa",        // name match (weight A)
                "Burkholderia cepacia complex",  // description match (weight B)
            ], [.. ordered.Select(x => x.Name)]);
            Assert.True(ordered[0].Rank > ordered[1].Rank);
        }

        /*=== pagination ===*/

        [Fact]
        public async Task SearchPathogen_FirstPage_SplitsMatchesWithMetadata_Success()
        {
            // 5 matches with page size 2 -> pages of [2, 2, 1]
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
                SearchTerm = "community acquired",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(2, result.Data.Items.Count());
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(2, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(3, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task SearchPathogen_LastPage_HasNoNext_Success()
        {
            // Upper boundary page: the single leftover match, no next page
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
                SearchTerm = "community acquired",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.True(item.Rank > 0);
            Assert.Equal(3, result.Data.Metadata.CurrentPage);
            Assert.True(result.Data.Metadata.HasPreviousPage);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task SearchPathogen_MinimumPageSize_ReturnsOneItemPerPage_Success()
        {
            // Lower boundary of a usable page size
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 1 },
                SearchTerm = "community acquired",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.True(item.Rank > 0);
            Assert.Equal(1, result.Data.Metadata.PageSize);
            Assert.Equal(5, result.Data.Metadata.PageCount);
            Assert.True(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task SearchPathogen_MaximumPageSize_ReturnsAllMatchesInOnePage_Success()
        {
            // Upper boundary of a usable page size
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 100 },
                SearchTerm = "community acquired",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            Assert.Equal(5, result.Data.Items.Count());
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);
            Assert.Equal(1, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        #endregion

        #region Empty result

        [Theory]
        [InlineData("a b")]      // every word is a single character
        [InlineData("x y z")]    // same, with more unusable words
        public async Task SearchPathogen_NoUsableLexeme_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                // The short-circuit always reports an empty first page of size 10
                Param = new PaginationParam { Page = 3, Size = 5 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.Equal(0, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasNextPage);
            Assert.False(result.Data.Metadata.HasPreviousPage);
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
            Assert.Equal(10, result.Data.Metadata.PageSize);
        }

        [Fact]
        public async Task SearchPathogen_TermMatchesNothing_ReturnsEmptyPage_Success()
        {
            await SeedMainAsync();

            // "norovirus" is a real respiratory pathogen but is not seeded here
            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "norovirus",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
            Assert.Equal(0, result.Data.Metadata.PageCount);
            Assert.False(result.Data.Metadata.HasNextPage);
        }

        [Fact]
        public async Task SearchPathogen_TermIsInsideNameButNotPrefix_ReturnsEmptyPage_Success()
        {
            // "ureus" appears inside "aureus" but matching is by prefix, so a term that
            // is only a suffix or an infix must not match
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "ureus",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        #region Soft-deleted pathogen

        [Fact]
        public async Task SearchPathogen_SoftDeletedPathogen_IsExcluded_Success()
        {
            await SeedMainAsync();

            // The soft deleted "Candida albicans" is the only pathogen that matches
            // "candida", so a non empty result would mean the query filter leaked
            var total = await _context.Pathogens.IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken);
            Assert.Equal(8, total);

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "candida",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        #region Symbol-only search term

        /*
         * FullTextSearchUtil.BuildPrefixQuery sanitises every character that is not a
         * letter, a digit or a hyphen, so a symbol-only term produces an empty tsquery
         * and the handler short-circuits to an empty page instead of sending malformed
         * tsquery syntax (& | ! ( ) : *) to PostgreSQL.
         *
         * These terms are also rejected by SearchPathogenValidator, so in the normal
         * pipeline this is defence in depth for the shared utility.
         */
        [Theory]
        [InlineData("&&")]
        [InlineData("||")]
        [InlineData("!!")]
        [InlineData("((")]
        public async Task SearchPathogen_SymbolOnlyTerm_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedMainAsync();

            var result = await _handler.HandleAsync(new SearchPathogenQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        /*
         * Fail path: SearchPathogenHandler has no failure branch. It only ever returns
         * a success result (an empty page when nothing matches or the term has no
         * usable lexeme). Input validation happens earlier in SearchPathogenValidator,
         * and the full text query cannot fail for sanitised input, so there is no
         * reachable failure result to assert here
         */
    }
}
