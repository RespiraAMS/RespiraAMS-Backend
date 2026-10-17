using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Antibiotics.SearchAntibiotic;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Antibiotics.SearchAntibiotic
{
    public class SearchAntibioticHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly SearchAntibioticHandler _handler;
        private readonly IDbContext _context;

        public SearchAntibioticHandlerTest(PostgresFixture fixture)
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
            // Dosages and antibiotics reference groups through FKs, so delete them first.
            // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.Dosages.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Antibiotics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private async Task<AntibioticGroup> SeedGroupAsync(string name, string description)
        {
            var group = new AntibioticGroup
            {
                Name = name,
                Description = description,
                ParentId = null,
            };
            await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return group;
        }

        private async Task<Antibiotic> SeedAntibioticAsync(
            string name, Guid antibioticGroupId, AwareClassification classification, bool softDeleted = false)
        {
            var antibiotic = new Antibiotic
            {
                Name = name,
                AntibioticGroupId = antibioticGroupId,
                Classification = classification,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.Antibiotics.AddAsync(antibiotic, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return antibiotic;
        }

        /*
         * Seeds the five "Cef" cephalosporins used by the pagination, classification and
         * ranking tests. Every name starts with "Cef", so the term "cef" matches all five
         */
        private async Task<AntibioticGroup> SeedCephalosporinsAsync()
        {
            var group = await SeedGroupAsync("Cephalosporins",
                "Beta-lactam antibiotics resistant to staphylococcal beta-lactamase");

            await SeedAntibioticAsync("Cefazolin", group.Id, AwareClassification.Access);
            await SeedAntibioticAsync("Cefuroxime", group.Id, AwareClassification.Access);
            await SeedAntibioticAsync("Cefotaxime", group.Id, AwareClassification.Watch);
            await SeedAntibioticAsync("Ceftriaxone", group.Id, AwareClassification.Watch);
            await SeedAntibioticAsync("Cefepime", group.Id, AwareClassification.Watch);

            return group;
        }

        #region Happy path

        [Fact]
        public async Task SearchAntibiotic_PrefixMatchOnName_ReturnsAntibioticWithGroupAndClassification_Success()
        {
            var group = await SeedGroupAsync("Beta-lactams",
                "Cell wall synthesis inhibitors sharing the beta-lactam ring");
            var antibiotic = await SeedAntibioticAsync("Amoxicillin", group.Id, AwareClassification.Access);

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "amox",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // "amox" is a prefix of the stored lexeme "amoxicillin"
            var item = Assert.Single(result.Data.Items);
            Assert.Equal(antibiotic.Id, item.Id);
            Assert.Equal("Amoxicillin", item.Name);
            // The nested group projection is loaded through the navigation property
            Assert.Equal(group.Id, item.AntibioticGroup.Id);
            Assert.Equal("Beta-lactams", item.AntibioticGroup.Name);
            Assert.Equal(AwareClassification.Access, item.Classification);
            Assert.True(item.Rank > 0);

            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
        }

        [Theory]
        [InlineData("amoxicillin")] // full word
        [InlineData("amoxi")]       // shorter prefix still matches
        [InlineData("AMOX")]        // the respira_vi configuration lowercases both sides
        public async Task SearchAntibiotic_PrefixTerm_MatchesSameAntibiotic_Success(string searchTerm)
        {
            var group = await SeedGroupAsync("Beta-lactams",
                "Cell wall synthesis inhibitors sharing the beta-lactam ring");
            await SeedAntibioticAsync("Amoxicillin", group.Id, AwareClassification.Access);

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Amoxicillin", item.Name);
        }

        [Fact]
        public async Task SearchAntibiotic_MatchesInDifferentGroups_ProjectsEachGroup_Success()
        {
            var macrolides = await SeedGroupAsync("Macrolides",
                "Protein synthesis inhibitors with a macrocyclic lactone ring");
            var monobactams = await SeedGroupAsync("Monobactams",
                "Beta-lactam antibiotics active only against gram-negative aerobes");
            await SeedAntibioticAsync("Azithromycin", macrolides.Id, AwareClassification.Watch);
            await SeedAntibioticAsync("Aztreonam", monobactams.Id, AwareClassification.Watch);

            // "az" is a prefix of both names
            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "az",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Equal(2, result.Data.Metadata.TotalItemCount);
            var azithromycin = result.Data.Items.Single(x => x.Name == "Azithromycin");
            Assert.Equal(macrolides.Id, azithromycin.AntibioticGroup.Id);
            Assert.Equal("Macrolides", azithromycin.AntibioticGroup.Name);

            var aztreonam = result.Data.Items.Single(x => x.Name == "Aztreonam");
            Assert.Equal(monobactams.Id, aztreonam.AntibioticGroup.Id);
            Assert.Equal("Monobactams", aztreonam.AntibioticGroup.Name);
        }

        [Fact]
        public async Task SearchAntibiotic_MatchesInSameGroup_CarriesGroupAndClassification_Success()
        {
            var group = await SeedCephalosporinsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "cef",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(5, result.Data.Metadata.TotalItemCount);

            var expected = new Dictionary<string, AwareClassification>
            {
                ["Cefazolin"] = AwareClassification.Access,
                ["Cefuroxime"] = AwareClassification.Access,
                ["Cefotaxime"] = AwareClassification.Watch,
                ["Ceftriaxone"] = AwareClassification.Watch,
                ["Cefepime"] = AwareClassification.Watch,
            };
            Assert.Equal(5, result.Data.Items.Count());
            foreach (var item in result.Data.Items)
            {
                Assert.Equal(expected[item.Name], item.Classification);
                Assert.Equal(group.Id, item.AntibioticGroup.Id);
                Assert.Equal("Cephalosporins", item.AntibioticGroup.Name);
            }
        }

        [Fact]
        public async Task SearchAntibiotic_Matches_OrderedByRankDescending_Success()
        {
            await SeedCephalosporinsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "cef",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);

            // Every single lexeme name ties on rank, so only the descending order is
            // asserted, not a specific permutation
            var ranks = result.Data.Items.Select(x => x.Rank).ToList();
            Assert.Equal(5, ranks.Count);
            Assert.All(ranks, rank => Assert.True(rank > 0));
            Assert.True(ranks.SequenceEqual(ranks.OrderByDescending(rank => rank)));
        }

        /*=== pagination ===*/

        [Fact]
        public async Task SearchAntibiotic_FirstPage_SplitsMatchesWithMetadata_Success()
        {
            // 5 matches with page size 2 -> pages of [2, 2, 1]
            await SeedCephalosporinsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
                SearchTerm = "cef",
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
        public async Task SearchAntibiotic_LastPage_HasNoNext_Success()
        {
            // Upper boundary page: the single leftover match, no next page
            await SeedCephalosporinsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
                SearchTerm = "cef",
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
        public async Task SearchAntibiotic_MinimumPageSize_ReturnsOneItemPerPage_Success()
        {
            // Lower boundary of a usable page size
            await SeedCephalosporinsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 1 },
                SearchTerm = "cef",
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
        public async Task SearchAntibiotic_MaximumPageSize_ReturnsAllMatchesInOnePage_Success()
        {
            // Upper boundary of a usable page size
            await SeedCephalosporinsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 100 },
                SearchTerm = "cef",
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
        public async Task SearchAntibiotic_NoUsableLexeme_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedCephalosporinsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
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
        public async Task SearchAntibiotic_TermMatchesNothing_ReturnsEmptyPage_Success()
        {
            await SeedCephalosporinsAsync();

            // "vancomycin" is a real antibiotic name but is not seeded here
            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "vancomycin",
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
        public async Task SearchAntibiotic_TermIsInsideNameButNotPrefix_ReturnsEmptyPage_Success()
        {
            // "mycin" appears inside "Azithromycin" but matching is by prefix, so a
            // term that is only a suffix or an infix must not match
            var group = await SeedGroupAsync("Macrolides",
                "Protein synthesis inhibitors with a macrocyclic lactone ring");
            await SeedAntibioticAsync("Azithromycin", group.Id, AwareClassification.Watch);

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "mycin",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            Assert.Empty(result.Data.Items);
            Assert.Equal(0, result.Data.Metadata.TotalItemCount);
        }

        #endregion

        #region Soft-deleted antibiotic

        [Fact]
        public async Task SearchAntibiotic_SoftDeletedAntibiotic_IsExcluded_Success()
        {
            // A soft-deleted antibiotic is hidden by the query filter and must never be
            // returned even when the search term matches its name
            var group = await SeedGroupAsync("Beta-lactams",
                "Cell wall synthesis inhibitors sharing the beta-lactam ring");
            await SeedAntibioticAsync("Amoxicillin", group.Id, AwareClassification.Access,
                softDeleted: true);

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "amox",
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
         * These terms are also rejected by SearchAntibioticValidator, so in the normal
         * pipeline this is defence in depth for the shared utility.
         */
        [Theory]
        [InlineData("&&")]
        [InlineData("||")]
        [InlineData("!!")]
        [InlineData("((")]
        public async Task SearchAntibiotic_SymbolOnlyTerm_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedCephalosporinsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticQuery
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
         * Fail path: SearchAntibioticHandler has no failure branch. It only ever returns
         * a success result (an empty page when nothing matches or the term has no usable
         * lexeme). Input validation happens earlier in SearchAntibioticValidator, and
         * the full text query cannot fail for sanitised input, so there is no reachable
         * failure result to assert here
         */
    }
}
