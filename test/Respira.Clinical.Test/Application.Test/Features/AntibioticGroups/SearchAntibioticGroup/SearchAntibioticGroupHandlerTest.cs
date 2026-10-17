using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.AntibioticGroups.SearchAntibioticGroup;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Infrastructure.Data;
using Respira.Clinical.Infrastructure.Mapper;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.AntibioticGroups.SearchAntibioticGroup
{
    public class SearchAntibioticGroupHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly SearchAntibioticGroupHandler _handler;
        private readonly IDbContext _context;

        public SearchAntibioticGroupHandlerTest(PostgresFixture fixture)
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
            // Children reference parents through a self FK, so delete them first.
            // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .Where(x => x.ParentId != null)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private async Task<AntibioticGroup> SeedGroupAsync(
            string name, string description, Guid? parentId = null, bool softDeleted = false)
        {
            var group = new AntibioticGroup
            {
                Name = name,
                Description = description,
                ParentId = parentId,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
            await _context.AntibioticGroups.AddAsync(group, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return group;
        }

        /*
         * Seeds a beta-lactam hierarchy: two root groups and two subgroups below
         * "Beta-lactams". Root groups must report a null parent while subgroups carry
         * the parent's name through the navigation projection
         */
        private async Task SeedBetaLactamHierarchyAsync()
        {
            var root = await SeedGroupAsync("Beta-lactams",
                "Cell wall synthesis inhibitors sharing the beta-lactam ring");
            await SeedGroupAsync("Penicillins",
                "Beta-lactam antibiotics active against gram-positive organisms", root.Id);
            await SeedGroupAsync("Cephalosporins",
                "Beta-lactam antibiotics resistant to staphylococcal beta-lactamase", root.Id);
            await SeedGroupAsync("Macrolides",
                "Protein synthesis inhibitors with a macrocyclic lactone ring");
        }

        /*
         * Seeds 5 groups whose descriptions all contain the word "antibiotic" so a
         * single term produces a deterministic match count for pagination boundaries
         */
        private async Task SeedFiveAntibioticGroupsAsync()
        {
            await SeedGroupAsync("Penicillins",
                "Beta-lactam antibiotic active against gram-positive organisms");
            await SeedGroupAsync("Cephalosporins",
                "Beta-lactam antibiotic resistant to staphylococcal beta-lactamase");
            await SeedGroupAsync("Macrolides",
                "Antibiotic that inhibits bacterial protein synthesis");
            await SeedGroupAsync("Tetracyclines",
                "Broad-spectrum antibiotic used for atypical infections");
            await SeedGroupAsync("Aminoglycosides",
                "Bactericidal antibiotic used for severe gram-negative infections");
        }

        #region Happy path

        [Fact]
        public async Task SearchAntibioticGroup_PrefixMatchOnName_ReturnsGroupWithParentName_Success()
        {
            await SeedBetaLactamHierarchyAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "penicillin",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // "penicillin" is a prefix of the stored lexeme "penicillins"
            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Penicillins", item.Name);
            Assert.Equal("Beta-lactam antibiotics active against gram-positive organisms", item.Description);
            Assert.NotNull(item.ParentId);
            Assert.Equal("Beta-lactams", item.ParentName);
            Assert.True(item.Rank > 0);

            Assert.Equal(1, result.Data.Metadata.TotalItemCount);
            Assert.Equal(1, result.Data.Metadata.CurrentPage);
        }

        [Fact]
        public async Task SearchAntibioticGroup_RootGroupMatch_HasNoParent_Success()
        {
            await SeedBetaLactamHierarchyAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "macrolide",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // A root group has no parent, so both parent fields must stay null
            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Macrolides", item.Name);
            Assert.Null(item.ParentId);
            Assert.Null(item.ParentName);
        }

        [Theory]
        [InlineData("penicillin")]      // full word prefix
        [InlineData("peni")]            // shorter prefix still matches
        [InlineData("  penicillin  ")]  // padding is discarded by whitespace splitting
        public async Task SearchAntibioticGroup_PrefixOrPaddedTerm_MatchesSameGroup_Success(string searchTerm)
        {
            await SeedBetaLactamHierarchyAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Penicillins", item.Name);
        }

        [Fact]
        public async Task SearchAntibioticGroup_NameMatchRanksAboveDescriptionMatch_Success()
        {
            await SeedGroupAsync("Vancomycin",
                "Glycopeptide antibiotic active against methicillin-resistant Staphylococcus aureus");
            await SeedGroupAsync("Glycopeptides",
                "Includes vancomycin and teicoplanin as the main representatives");

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "vancomycin",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            // The term appears in "Vancomycin" name (weight A) and in "Glycopeptides"
            // description (weight B): name relevance must come first
            Assert.Equal(["Vancomycin", "Glycopeptides"], [.. result.Data.Items.Select(x => x.Name)]);
            var nameMatch = result.Data.Items.Single(x => x.Name == "Vancomycin");
            var descriptionMatch = result.Data.Items.Single(x => x.Name == "Glycopeptides");
            Assert.True(nameMatch.Rank > descriptionMatch.Rank);
            Assert.True(descriptionMatch.Rank > 0);
        }

        [Fact]
        public async Task SearchAntibioticGroup_MatchOnlyInDescription_ReturnsGroup_Success()
        {
            await SeedBetaLactamHierarchyAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "macrocyclic",
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Macrolides", item.Name);
            Assert.True(item.Rank > 0);
        }

        [Theory]
        [InlineData("khang")]  // unaccented query term
        [InlineData("kháng")]  // accented query term
        public async Task SearchAntibioticGroup_AccentFoldedTerm_MatchesVietnameseData_Success(string searchTerm)
        {
            await SeedGroupAsync("Kháng sinh dự phòng",
                "Kháng sinh sử dụng để dự phòng nhiễm khuẩn sau phẫu thuật");

            // Both the stored vector and the query run through the respira_vi
            // configuration, which folds accents with unaccent
            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = searchTerm,
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var item = Assert.Single(result.Data.Items);
            Assert.Equal("Kháng sinh dự phòng", item.Name);
        }

        /*=== pagination ===*/

        [Fact]
        public async Task SearchAntibioticGroup_FirstPage_SplitsMatchesWithMetadata_Success()
        {
            // 5 matches with page size 2 -> pages of [2, 2, 1]
            await SeedFiveAntibioticGroupsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 2 },
                SearchTerm = "antibiotic",
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
        public async Task SearchAntibioticGroup_LastPage_HasNoNext_Success()
        {
            // Upper boundary page: the single leftover match, no next page
            await SeedFiveAntibioticGroupsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 3, Size = 2 },
                SearchTerm = "antibiotic",
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
        public async Task SearchAntibioticGroup_MinimumPageSize_ReturnsOneItemPerPage_Success()
        {
            // Lower boundary of a usable page size
            await SeedFiveAntibioticGroupsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 1 },
                SearchTerm = "antibiotic",
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
        public async Task SearchAntibioticGroup_MaximumPageSize_ReturnsAllMatchesInOnePage_Success()
        {
            // Upper boundary of a usable page size
            await SeedFiveAntibioticGroupsAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 100 },
                SearchTerm = "antibiotic",
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
        public async Task SearchAntibioticGroup_NoUsableLexeme_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedBetaLactamHierarchyAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
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
        public async Task SearchAntibioticGroup_TermMatchesNothing_ReturnsEmptyPage_Success()
        {
            await SeedBetaLactamHierarchyAsync();

            // "vancomycin" is a real antibiotic group name but is not seeded here
            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
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

        #endregion

        #region Soft-deleted group

        [Fact]
        public async Task SearchAntibioticGroup_SoftDeletedGroup_IsExcluded_Success()
        {
            // A soft-deleted group is hidden by the query filter and must never be
            // returned even when the search term matches its name
            await SeedGroupAsync("Penicillins",
                "Beta-lactam antibiotics active against gram-positive organisms", softDeleted: true);

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam { Page = 1, Size = 10 },
                SearchTerm = "penicillin",
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
         * These terms are also rejected by SearchAntibioticGroupValidator, so in the
         * normal pipeline this is defence in depth for the shared utility.
         */
        [Theory]
        [InlineData("&&")]
        [InlineData("||")]
        [InlineData("!!")]
        [InlineData("((")]
        public async Task SearchAntibioticGroup_SymbolOnlyTerm_ReturnsEmptyPage_Success(string searchTerm)
        {
            await SeedBetaLactamHierarchyAsync();

            var result = await _handler.HandleAsync(new SearchAntibioticGroupQuery
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
         * Fail path: SearchAntibioticGroupHandler has no failure branch. It only ever
         * returns a success result (an empty page when nothing matches or the term has
         * no usable lexeme). Input validation happens earlier in
         * SearchAntibioticGroupValidator, and the full text query cannot fail for
         * sanitised input, so there is no reachable failure result to assert here
         */
    }
}
