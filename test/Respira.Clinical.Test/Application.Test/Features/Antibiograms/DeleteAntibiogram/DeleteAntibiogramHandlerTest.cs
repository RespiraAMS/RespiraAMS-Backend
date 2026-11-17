using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Antibiograms.DeleteAntibiogram;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.DeleteAntibiogram
{
    public class DeleteAntibiogramHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteAntibiogramHandler _handler;
        private readonly IDbContext _context;

        public DeleteAntibiogramHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteAntibiogramHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
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
         * request.
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

        private async Task<Antibiogram> SeedAntibiogramAsync(
            Guid pathogenId,
            MicCategory micCategory,
            Guid[] antibioticIds,
            bool softDeleted = false)
        {
            await using var seedContext = new ClinicalDbContext(_options);
            var antibiotics = await seedContext.Antibiotics
                .Where(x => antibioticIds.Contains(x.Id))
                .ToListAsync(TestContext.Current.CancellationToken);

            var antibiogram = new Antibiogram
            {
                PathogenId = pathogenId,
                MicCategory = micCategory,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
                Antibiotics = [.. antibiotics],
            };

            await seedContext.Antibiograms.AddAsync(
                antibiogram, TestContext.Current.CancellationToken);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            return antibiogram;
        }

        private async Task<Antibiogram> LoadAntibiogramAsync(Guid id, bool ignoreQueryFilters = false)
        {
            // Fresh context so the handler's change tracker cannot mask a failed commit
            await using var freshContext = new ClinicalDbContext(_options);
            var query = freshContext.Antibiograms.AsQueryable();
            if (ignoreQueryFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            return await query
                .Include(x => x.Antibiotics)
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<int> CountSoftDeletedAntibiogramsAsync()
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.Antibiograms.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken);
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task DeleteAntibiogram_SingleAntibiotic_Success()
        {
            /*
             * Boundary shape: exactly 1 antibiotic, the minimum realistic panel for
             * a susceptibility result. Streptococcus pneumoniae is the most common
             * cause of community-acquired pneumonia and ceftriaxone is a standard
             * treatment
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var macrolides = Group("Macrolides");

            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var azithromycin = Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch);

            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var klebsiella = Pathogen("Klebsiella pneumoniae", isAtypical: false);

            await SeedAsync(
                betaLactams, cephalosporins, macrolides,
                ceftriaxone, azithromycin,
                pneumococcus, klebsiella);

            var target = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [ceftriaxone.Id]);
            var control = await SeedAntibiogramAsync(
                klebsiella.Id, MicCategory.Resistant, [azithromycin.Id]);
            var deletedBefore = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(
                new DeleteAntibiogramCommand { Id = target.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // Business rule: the antibiogram is soft deleted, never removed
            var deleted = await LoadAntibiogramAsync(target.Id, ignoreQueryFilters: true);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
            Assert.InRange(
                deleted.DeletedAt.Value,
                deletedBefore.AddSeconds(-5),
                DateTimeOffset.UtcNow.AddSeconds(5));

            // The default query filter must now hide the deleted antibiogram
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.False(await freshContext.Antibiograms
                .AnyAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken));

            // Business rule: soft delete keeps the susceptibility joins intact
            var antibiotic = Assert.Single(deleted.Antibiotics);
            Assert.Equal(ceftriaxone.Id, antibiotic.Id);

            // Business rule: the referenced pathogen and antibiotics are not cascaded
            var keptPathogen = await freshContext.Pathogens
                .SingleAsync(x => x.Id == pneumococcus.Id, TestContext.Current.CancellationToken);
            Assert.False(keptPathogen.IsDeleted);
            Assert.False((await freshContext.Antibiotics
                .SingleAsync(x => x.Id == ceftriaxone.Id, TestContext.Current.CancellationToken)).IsDeleted);

            // The unrelated antibiogram stays live
            var untouched = await LoadAntibiogramAsync(control.Id);
            Assert.False(untouched.IsDeleted);
            Assert.Null(untouched.DeletedAt);
        }

        [Fact]
        public async Task DeleteAntibiogram_MultipleAntibiotics_Success()
        {
            /*
             * A realistic susceptibility panel for community-acquired pneumonia:
             * a third-generation cephalosporin, a carbapenem and a macrolide. All
             * three joins must survive the soft delete
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

            var target = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Intermediate,
                [ceftriaxone.Id, meropenem.Id, azithromycin.Id]);

            var result = await _handler.HandleAsync(
                new DeleteAntibiogramCommand { Id = target.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            var deleted = await LoadAntibiogramAsync(target.Id, ignoreQueryFilters: true);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);

            // Boundary count above the minimum: every join is retained
            Assert.Equal(3, deleted.Antibiotics.Count);
            Assert.Equal(
                new[] { ceftriaxone.Id, meropenem.Id, azithromycin.Id }.Order(),
                deleted.Antibiotics.Select(x => x.Id).Order());
        }

        #endregion

        #region Fail path

        /*
         * Note: this feature has no mapper, so there is no map-failure branch to
         * cover. The soft delete has no reference checks either: an antibiogram is
         * a leaf entity, so no business-rule violation path exists.
         */

        [Fact]
        public async Task DeleteAntibiogram_UnknownId_Fail()
        {
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var target = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);

            var result = await _handler.HandleAsync(
                new DeleteAntibiogramCommand { Id = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing must be soft-deleted when the target does not exist
            Assert.Equal(0, await CountSoftDeletedAntibiogramsAsync());
            var untouched = await LoadAntibiogramAsync(target.Id);
            Assert.False(untouched.IsDeleted);
            Assert.Null(untouched.DeletedAt);
        }

        [Fact]
        public async Task DeleteAntibiogram_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never identify a stored antibiogram
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var target = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);

            var result = await _handler.HandleAsync(
                new DeleteAntibiogramCommand { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            Assert.Equal(0, await CountSoftDeletedAntibiogramsAsync());
            Assert.False((await LoadAntibiogramAsync(target.Id)).IsDeleted);
        }

        [Fact]
        public async Task DeleteAntibiogram_AlreadyDeleted_Fail()
        {
            /*
             * The !IsDeleted query filter hides soft-deleted antibiograms, so a
             * retried delete is reported as a missing antibiogram and must not move
             * the original deletion timestamp
             */
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var deleted = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id],
                softDeleted: true);
            var control = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Resistant, [amoxicillin.Id]);
            var before = await LoadAntibiogramAsync(deleted.Id, ignoreQueryFilters: true);

            var result = await _handler.HandleAsync(
                new DeleteAntibiogramCommand { Id = deleted.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The handler bails out before soft deleting again: the original
            // deletion timestamp and the joins stay exactly as they were
            var after = await LoadAntibiogramAsync(deleted.Id, ignoreQueryFilters: true);
            Assert.True(after.IsDeleted);
            Assert.Equal(before.DeletedAt, after.DeletedAt);
            Assert.Equal(
                before.Antibiotics.Select(x => x.Id).Order(),
                after.Antibiotics.Select(x => x.Id).Order());

            // The unrelated antibiogram is untouched
            Assert.False((await LoadAntibiogramAsync(control.Id)).IsDeleted);
        }

        #endregion
    }
}
