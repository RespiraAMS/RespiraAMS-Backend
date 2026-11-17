using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Antibiograms.UpdateAntibiogram;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Antibiograms.UpdateAntibiogram
{
    public class UpdateAntibiogramHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly UpdateAntibiogramHandler _handler;
        private readonly IDbContext _context;

        public UpdateAntibiogramHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var mapper = new UpdateAntibiogramMapper();
            var logger = new Mock<ILogger<UpdateAntibiogramHandler>>().Object;

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
                // A past timestamp makes it unambiguous that an update refreshes it
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-30),
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

        private static void AssertUnchanged(Antibiogram before, Antibiogram after)
        {
            Assert.Equal(before.PathogenId, after.PathogenId);
            Assert.Equal(before.MicCategory, after.MicCategory);
            Assert.Equal(before.IsDeleted, after.IsDeleted);
            Assert.Equal(before.DeletedAt, after.DeletedAt);
            // PostgreSQL truncates timestamps to microsecond precision, so both
            // snapshots are compared as they were reloaded from the database
            Assert.Equal(before.UpdatedAt, after.UpdatedAt);
            Assert.Equal(
                before.Antibiotics.Select(x => x.Id).Order(),
                after.Antibiotics.Select(x => x.Id).Order());
        }

        #endregion

        #region Happy path

        // Boundary values of MicCategory: the lowest and highest defined members
        [Theory]
        [InlineData(MicCategory.Susceptible)]
        [InlineData(MicCategory.Resistant)]
        public async Task UpdateAntibiogram_MinimalValues_Success(MicCategory micCategory)
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
            var legionella = Pathogen("Legionella pneumophila", isAtypical: true);

            await SeedAsync(
                betaLactams, cephalosporins, ceftriaxone,
                pneumococcus, legionella);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Intermediate, [ceftriaxone.Id]);
            var updatedBefore = DateTimeOffset.UtcNow;

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = legionella.Id,
                MicCategory = micCategory,
                AntibioticIds = [ceftriaxone.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await LoadAntibiogramAsync(seeded.Id);

            // Business rule: the susceptibility result is overwritten in place
            Assert.Equal(seeded.Id, saved.Id);
            Assert.Equal(legionella.Id, saved.PathogenId);
            Assert.Equal(micCategory, saved.MicCategory);
            Assert.False(saved.IsDeleted);
            Assert.Null(saved.DeletedAt);

            // Business rule: the update timestamp is refreshed
            Assert.True(saved.UpdatedAt > seeded.UpdatedAt);
            Assert.InRange(
                saved.UpdatedAt,
                updatedBefore.AddSeconds(-5),
                DateTimeOffset.UtcNow.AddSeconds(5));

            var antibiotic = Assert.Single(saved.Antibiotics);
            Assert.Equal(ceftriaxone.Id, antibiotic.Id);
            Assert.Equal("Ceftriaxone", antibiotic.Name);
        }

        [Fact]
        public async Task UpdateAntibiogram_AddAntibiotic_Success()
        {
            /*
             * A susceptibility panel grows when another antibiotic is tested. The
             * already linked ceftriaxone must be kept and azithromycin added
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var macrolides = Group("Macrolides");

            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var azithromycin = Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);

            await SeedAsync(
                betaLactams, cephalosporins, macrolides,
                ceftriaxone, azithromycin,
                pneumococcus);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [ceftriaxone.Id]);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Intermediate,
                AntibioticIds = [ceftriaxone.Id, azithromycin.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await LoadAntibiogramAsync(seeded.Id);
            Assert.Equal(2, saved.Antibiotics.Count);
            Assert.Equal(
                new[] { ceftriaxone.Id, azithromycin.Id }.Order(),
                saved.Antibiotics.Select(x => x.Id).Order());
        }

        [Fact]
        public async Task UpdateAntibiogram_RemoveAntibiotic_Success()
        {
            // The complement of adding: an antibiotic can be dropped from the panel
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var macrolides = Group("Macrolides");

            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var azithromycin = Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);

            await SeedAsync(
                betaLactams, cephalosporins, macrolides,
                ceftriaxone, azithromycin,
                pneumococcus);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Resistant,
                [ceftriaxone.Id, azithromycin.Id]);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [ceftriaxone.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await LoadAntibiogramAsync(seeded.Id);
            var antibiotic = Assert.Single(saved.Antibiotics);
            Assert.Equal(ceftriaxone.Id, antibiotic.Id);
            Assert.DoesNotContain(saved.Antibiotics, x => x.Id == azithromycin.Id);
        }

        [Fact]
        public async Task UpdateAntibiogram_ReplaceAntibiotics_Success()
        {
            /*
             * Correcting a susceptibility panel replaces the tested set: the old
             * ceftriaxone link must disappear and exactly the two new antibiotics
             * must remain
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

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [ceftriaxone.Id]);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [meropenem.Id, azithromycin.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await LoadAntibiogramAsync(seeded.Id);
            Assert.Equal(2, saved.Antibiotics.Count);
            Assert.Equal(
                new[] { meropenem.Id, azithromycin.Id }.Order(),
                saved.Antibiotics.Select(x => x.Id).Order());
            Assert.DoesNotContain(saved.Antibiotics, x => x.Id == ceftriaxone.Id);
        }

        [Fact]
        public async Task UpdateAntibiogram_SameLineageAntibiotics_Success()
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
            var macrolides = Group("Macrolides");

            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var meropenem = Antibiotic("Meropenem", carbapenems.Id, AwareClassification.Watch);
            var azithromycin = Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);

            await SeedAsync(
                betaLactams, cephalosporins, carbapenems, macrolides,
                ceftriaxone, meropenem, azithromycin,
                pneumococcus);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [azithromycin.Id]);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Intermediate,
                AntibioticIds = [ceftriaxone.Id, meropenem.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await LoadAntibiogramAsync(seeded.Id);
            Assert.Equal(2, saved.Antibiotics.Count);
            Assert.Equal(
                new[] { ceftriaxone.Id, meropenem.Id }.Order(),
                saved.Antibiotics.Select(x => x.Id).Order());
        }

        #endregion

        #region Fail path

        /*
         * Skipped: an empty or duplicated antibiotic ID list never reaches the
         * handler in the real pipeline, because UpdateAntibiogramValidator rejects
         * both before the handler runs. Both are covered in the validator tests.
         *
         * Skipped: the map-failure branch cannot be tested because
         * UpdateAntibiogramMapper always returns a successful result.
         */

        [Fact]
        public async Task UpdateAntibiogram_UnknownId_Fail()
        {
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var existing = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var before = await LoadAntibiogramAsync(existing.Id);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = Guid.CreateVersion7(),
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [amoxicillin.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The unrelated antibiogram must stay untouched
            AssertUnchanged(before, await LoadAntibiogramAsync(existing.Id));
        }

        [Fact]
        public async Task UpdateAntibiogram_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never identify a stored antibiogram
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var existing = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var before = await LoadAntibiogramAsync(existing.Id);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = Guid.Empty,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [amoxicillin.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            AssertUnchanged(before, await LoadAntibiogramAsync(existing.Id));
        }

        [Fact]
        public async Task UpdateAntibiogram_AlreadyDeleted_Fail()
        {
            // The !IsDeleted query filter hides soft-deleted antibiograms, so a
            // retried update is reported as a missing antibiogram
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var deleted = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id],
                softDeleted: true);
            var before = await LoadAntibiogramAsync(deleted.Id, ignoreQueryFilters: true);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = deleted.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [amoxicillin.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // The deletion flag and timestamp must not be modified
            var after = await LoadAntibiogramAsync(deleted.Id, ignoreQueryFilters: true);
            Assert.True(after.IsDeleted);
            Assert.NotNull(after.DeletedAt);
            AssertUnchanged(before, after);
        }

        [Fact]
        public async Task UpdateAntibiogram_UnknownPathogen_Fail()
        {
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var before = await LoadAntibiogramAsync(seeded.Id);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = Guid.CreateVersion7(),
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [amoxicillin.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing may be written when the new pathogen does not exist
            AssertUnchanged(before, await LoadAntibiogramAsync(seeded.Id));
        }

        [Fact]
        public async Task UpdateAntibiogram_SoftDeletedPathogen_Fail()
        {
            /*
             * A soft-deleted pathogen is hidden by the query filter, so referencing
             * it must be rejected just like an unknown pathogen
             */
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var deletedPathogen = Pathogen(
                "Legionella pneumophila", isAtypical: true, softDeleted: true);
            await SeedAsync(group, amoxicillin, pneumococcus, deletedPathogen);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var before = await LoadAntibiogramAsync(seeded.Id);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = deletedPathogen.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [amoxicillin.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            AssertUnchanged(before, await LoadAntibiogramAsync(seeded.Id));
        }

        [Fact]
        public async Task UpdateAntibiogram_UnknownAntibiotic_Fail()
        {
            /*
             * One existing antibiotic and one ID that does not exist: the update
             * must be rejected as a whole, not partially applied
             */
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var before = await LoadAntibiogramAsync(seeded.Id);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [amoxicillin.Id, Guid.CreateVersion7()],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            AssertUnchanged(before, await LoadAntibiogramAsync(seeded.Id));
        }

        [Fact]
        public async Task UpdateAntibiogram_AllAntibioticsUnknown_Fail()
        {
            // Boundary: 0 of the requested antibiotics exist, the lowest possible match
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var before = await LoadAntibiogramAsync(seeded.Id);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            AssertUnchanged(before, await LoadAntibiogramAsync(seeded.Id));
        }

        [Fact]
        public async Task UpdateAntibiogram_SoftDeletedAntibiotic_Fail()
        {
            // A soft-deleted antibiotic is hidden by the query filter, so referencing
            // it must be rejected just like an unknown antibiotic
            var group = Group("Carbapenems");
            var amoxicillinGroup = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", amoxicillinGroup.Id, AwareClassification.Access);
            var deletedMeropenem = Antibiotic(
                "Meropenem", group.Id, AwareClassification.Watch, softDeleted: true);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(amoxicillinGroup, group, amoxicillin, deletedMeropenem, pneumococcus);

            var seeded = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var before = await LoadAntibiogramAsync(seeded.Id);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = seeded.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Resistant,
                AntibioticIds = [deletedMeropenem.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            AssertUnchanged(before, await LoadAntibiogramAsync(seeded.Id));
        }

        #endregion

        #region Uniqueness

        [Fact]
        public async Task UpdateAntibiogram_DuplicatePathogenAndMic_Fail()
        {
            // Business rule: a pathogen can only have one antibiogram per MIC category
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var doxycycline = Antibiotic("Doxycycline", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var klebsiella = Pathogen("Klebsiella pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, doxycycline, pneumococcus, klebsiella);

            var conflicting = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var target = await SeedAntibiogramAsync(
                klebsiella.Id, MicCategory.Resistant, [doxycycline.Id]);
            var conflictingBefore = await LoadAntibiogramAsync(conflicting.Id);
            var targetBefore = await LoadAntibiogramAsync(target.Id);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = target.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Susceptible,
                AntibioticIds = [doxycycline.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.Error.StatusCode);

            // Neither the conflicting antibiogram nor the target may be modified
            AssertUnchanged(conflictingBefore, await LoadAntibiogramAsync(conflicting.Id));
            AssertUnchanged(targetBefore, await LoadAntibiogramAsync(target.Id));
        }

        [Fact]
        public async Task UpdateAntibiogram_SamePathogenDifferentMic_Success()
        {
            // Different MIC categories for the same pathogen are distinct records
            var group = Group("Carbapenems");
            var meropenem = Antibiotic("Meropenem", group.Id, AwareClassification.Watch);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            await SeedAsync(group, meropenem, pneumococcus);

            var susceptible = await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [meropenem.Id]);
            await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Resistant, [meropenem.Id]);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = susceptible.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Intermediate,
                AntibioticIds = [meropenem.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await LoadAntibiogramAsync(susceptible.Id);
            Assert.Equal(pneumococcus.Id, saved.PathogenId);
            Assert.Equal(MicCategory.Intermediate, saved.MicCategory);
        }

        [Fact]
        public async Task UpdateAntibiogram_DifferentPathogenSameMic_Success()
        {
            // Different pathogens may share the same MIC category
            var group = Group("Aminopenicillins");
            var amoxicillin = Antibiotic("Amoxicillin", group.Id, AwareClassification.Access);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var klebsiella = Pathogen("Klebsiella pneumoniae", isAtypical: false);
            await SeedAsync(group, amoxicillin, pneumococcus, klebsiella);

            await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [amoxicillin.Id]);
            var target = await SeedAntibiogramAsync(
                klebsiella.Id, MicCategory.Resistant, [amoxicillin.Id]);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = target.Id,
                PathogenId = klebsiella.Id,
                MicCategory = MicCategory.Susceptible,
                AntibioticIds = [amoxicillin.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await LoadAntibiogramAsync(target.Id);
            Assert.Equal(klebsiella.Id, saved.PathogenId);
            Assert.Equal(MicCategory.Susceptible, saved.MicCategory);
        }

        [Fact]
        public async Task UpdateAntibiogram_SoftDeletedConflict_Success()
        {
            // A soft-deleted antibiogram is hidden by the query filter, so it must
            // not block the same pathogen + MIC pair from being used again
            var group = Group("Carbapenems");
            var meropenem = Antibiotic("Meropenem", group.Id, AwareClassification.Watch);
            var pneumococcus = Pathogen("Streptococcus pneumoniae", isAtypical: false);
            var klebsiella = Pathogen("Klebsiella pneumoniae", isAtypical: false);
            await SeedAsync(group, meropenem, pneumococcus, klebsiella);

            await SeedAntibiogramAsync(
                pneumococcus.Id, MicCategory.Susceptible, [meropenem.Id], softDeleted: true);
            var target = await SeedAntibiogramAsync(
                klebsiella.Id, MicCategory.Resistant, [meropenem.Id]);

            var result = await _handler.HandleAsync(new UpdateAntibiogramCommand
            {
                Id = target.Id,
                PathogenId = pneumococcus.Id,
                MicCategory = MicCategory.Susceptible,
                AntibioticIds = [meropenem.Id],
            }, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Updated, result.StatusCode);

            var saved = await LoadAntibiogramAsync(target.Id);
            Assert.Equal(pneumococcus.Id, saved.PathogenId);
            Assert.Equal(MicCategory.Susceptible, saved.MicCategory);
        }

        #endregion
    }
}
