using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Antibiograms.CreateAntibiogram;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.CreateAntibiogram
{
    public class CreateAntibiogramHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly CreateAntibiogramHandler _handler;
        private readonly IDbContext _context;

        public CreateAntibiogramHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new CreateAntibiogramMapper();
            var logger = new Mock<ILogger<CreateAntibiogramHandler>>().Object;

            // Initialize handler
            _handler = new(_context, mapper, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the count
            // assertions are deterministic across runs. The antibiogram joins are
            // removed with their antibiogram (ON DELETE CASCADE). IgnoreQueryFilters
            // is needed because soft-deleted rows are hidden by the query filter but
            // still occupy the table
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
         * Reference data is written through its own context and saved before the
         * handler runs, so the handler context starts clean just like a real HTTP
         * request. This matters because the handler resolves antibiotics through
         * AttachStub, and a pre-populated change tracker would mask navigation
         * loading problems.
         */
        private async Task SeedAsync(params object[] entities)
        {
            await using var seedContext = new ClinicalDbContext(_options);
            seedContext.AddRange(entities);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
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

        private async Task<Antibiogram> LoadAntibiogramAsync(Guid id)
        {
            // Fresh context so the handler's change tracker cannot mask a failed commit
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.Antibiograms
                .Include(x => x.Antibiotics)
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task AssertNothingPersistedAsync()
        {
            // The join table rows are cascade-deleted with their antibiogram, so an
            // empty antibiogram table also means no orphaned joins were written
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.Equal(0, await freshContext.Antibiograms
                .IgnoreQueryFilters()
                .CountAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region Happy path

        // Boundary values of MicCategory: the lowest and highest defined members
        [Theory]
        [InlineData(MicCategory.Susceptible)]
        [InlineData(MicCategory.Resistant)]
        public async Task CreateAntibiogram_MinimalValues_Success(MicCategory micCategory)
        {
            /*
             * Boundary shape: exactly 1 antibiotic, the minimum accepted by the
             * command. Streptococcus pneumoniae is the most common cause of
             * community-acquired pneumonia and ceftriaxone is a standard treatment
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);

            await SeedAsync(betaLactams, cephalosporins, ceftriaxone, pneumococcus);

            var command = new CreateAntibiogramCommand
            {
                PathogenId = pneumococcus.Id,
                MicCategory = micCategory,
                AntibioticIds = [ceftriaxone.Id],
            };

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);
            Assert.NotEqual(Guid.Empty, result.Data.Id);

            var saved = await LoadAntibiogramAsync(result.Data.Id);

            // Business rule: the susceptibility test is stored for the chosen pathogen
            Assert.Equal(pneumococcus.Id, saved.PathogenId);
            Assert.Equal(micCategory, saved.MicCategory);
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);

            // Business rule: every requested antibiotic is linked to the antibiogram
            var antibiotic = Assert.Single(saved.Antibiotics);
            Assert.Equal(ceftriaxone.Id, antibiotic.Id);
            Assert.Equal("Ceftriaxone", antibiotic.Name);
        }

        [Fact]
        public async Task CreateAntibiogram_MultipleAntibiotics_Success()
        {
            /*
             * A realistic susceptibility panel for community-acquired pneumonia:
             * a third-generation cephalosporin, a carbapenem and a macrolide
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var carbapenems = Group("Carbapenems", betaLactams.Id);
            var macrolides = Group("Macrolides");

            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var meropenem = Antibiotic("Meropenem", carbapenems.Id, AwareClassification.Watch);
            var azithromycin = Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);

            await SeedAsync(
                betaLactams, cephalosporins, carbapenems, macrolides,
                ceftriaxone, meropenem, azithromycin,
                pneumococcus);

            var command = new CreateAntibiogramCommand
            {
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Intermediate,
                AntibioticIds = [ceftriaxone.Id, meropenem.Id, azithromycin.Id],
            };

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);

            var saved = await LoadAntibiogramAsync(result.Data.Id);

            Assert.Equal(pneumococcus.Id, saved.PathogenId);
            Assert.Equal(MicCategory.Intermediate, saved.MicCategory);

            // Boundary count above the minimum: all 3 antibiotics are persisted
            Assert.Equal(3, saved.Antibiotics.Count);
            Assert.Equal(
                new[] { ceftriaxone.Id, meropenem.Id, azithromycin.Id }.Order(),
                saved.Antibiotics.Select(x => x.Id).Order());
        }

        [Fact]
        public async Task CreateAntibiogram_SameLineageAntibiotics_Success()
        {
            /*
             * Business rule: the antibiotic list of an antibiogram has OR semantics -
             * it records every antibiotic the pathogen was tested against. Unlike a
             * treatment protocol composition, a combination is not rejected for
             * sharing a group lineage, so two beta-lactams must both be accepted
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var carbapenems = Group("Carbapenems", betaLactams.Id);

            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var meropenem = Antibiotic("Meropenem", carbapenems.Id, AwareClassification.Watch);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);

            await SeedAsync(
                betaLactams, cephalosporins, carbapenems,
                ceftriaxone, meropenem,
                pneumococcus);

            var command = new CreateAntibiogramCommand
            {
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [ceftriaxone.Id, meropenem.Id],
            };

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Created, result.StatusCode);

            var saved = await LoadAntibiogramAsync(result.Data.Id);
            Assert.Equal(2, saved.Antibiotics.Count);
            Assert.Equal(
                new[] { ceftriaxone.Id, meropenem.Id }.Order(),
                saved.Antibiotics.Select(x => x.Id).Order());
        }

        #endregion

        #region Fail path

        /*
         * Skipped: an empty or duplicated antibiotic ID list never reaches the
         * handler in the real pipeline, because CreateAntibiogramValidator rejects
         * both before the handler runs. Both are covered in the validator tests.
         *
         * Skipped: the map-failure branch cannot be tested because
         * CreateAntibiogramMapper always returns a successful result.
         */

        [Fact]
        public async Task CreateAntibiogram_UnknownPathogen_Fail()
        {
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            await SeedAsync(group, amoxicillin);

            var command = new CreateAntibiogramCommand
            {
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Susceptible,
                AntibioticIds = [amoxicillin.Id],
            };

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await AssertNothingPersistedAsync();
        }

        [Fact]
        public async Task CreateAntibiogram_SoftDeletedPathogen_Fail()
        {
            /*
             * A soft-deleted pathogen is hidden by the query filter, so referencing
             * it must be rejected just like an unknown pathogen
             */
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var deletedPathogen = Pathogen(
                "Legionella pneumophila", isAtypical: true, softDeleted: true);
            await SeedAsync(group, amoxicillin, deletedPathogen);

            var command = new CreateAntibiogramCommand
            {
                PathogenId = deletedPathogen.Id,
                MicCategory = MicCategory.Susceptible,
                AntibioticIds = [amoxicillin.Id],
            };

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await AssertNothingPersistedAsync();
        }

        [Fact]
        public async Task CreateAntibiogram_UnknownAntibiotic_Fail()
        {
            /*
             * One existing antibiotic and one ID that does not exist: the antibiogram
             * must be rejected as a whole, not partially created
             */
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var command = new CreateAntibiogramCommand
            {
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Susceptible,
                AntibioticIds = [amoxicillin.Id, Guid.CreateVersion7()],
            };

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await AssertNothingPersistedAsync();
        }

        [Fact]
        public async Task CreateAntibiogram_AllAntibioticsUnknown_Fail()
        {
            // Boundary: 0 of the requested antibiotics exist, the lowest possible match
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(pneumococcus);

            var command = new CreateAntibiogramCommand
            {
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Susceptible,
                AntibioticIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
            };

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await AssertNothingPersistedAsync();
        }

        [Fact]
        public async Task CreateAntibiogram_SoftDeletedAntibiotic_Fail()
        {
            // A soft-deleted antibiotic is hidden by the query filter, so referencing
            // it must be rejected just like an unknown antibiotic
            var group = Group("Carbapenems");
            var meropenem = Antibiotic(
                "Meropenem", group.Id, AwareClassification.Watch, softDeleted: true);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, meropenem, pneumococcus);

            var command = new CreateAntibiogramCommand
            {
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [meropenem.Id],
            };

            var result = await _handler.HandleAsync(
                command, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Null(result.Data);

            await AssertNothingPersistedAsync();
        }

        #endregion
    }
}
