using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Diagnosis.WarnAntibioticCombination;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Domain.Services;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.Diagnosis.WarnAntibioticCombination
{
    public class WarnAntibioticCombinationHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly WarnAntibioticCombinationHandler _handler;
        private readonly IDbContext _context;

        public WarnAntibioticCombinationHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var service = new DiagnoseService(new Mock<ILogger<DiagnoseService>>().Object);
            var logger = new Mock<ILogger<WarnAntibioticCombinationHandler>>().Object;

            // Initialize handler
            _handler = new(_context, service, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so every test
            // starts from a known state. IgnoreQueryFilters is needed because soft
            // deleted rows are hidden by the query filter but still occupy the table
            await _context.Antibiotics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            // A group can reference its parent, so delete sub-groups before parents
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .Where(x => x.ParentId != null)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        private sealed record SeedData(
            AntibioticGroup BetaLactams,
            AntibioticGroup Penicillins,
            AntibioticGroup Aminopenicillins,
            AntibioticGroup Cephalosporins,
            AntibioticGroup Carbapenems,
            AntibioticGroup Macrolides,
            AntibioticGroup Fluoroquinolones,
            AntibioticGroup Glycopeptides,
            AntibioticGroup Oxazolidinones,
            Antibiotic Amoxicillin,
            Antibiotic Ampicillin,
            Antibiotic Benzylpenicillin,
            Antibiotic Ceftriaxone,
            Antibiotic Meropenem,
            Antibiotic Azithromycin,
            Antibiotic Levofloxacin,
            Antibiotic Vancomycin,
            Antibiotic Linezolid,
            Antibiotic RetiredAntibiotic);

        private static AntibioticGroup Group(string name, Guid? parentId = null)
        {
            return new AntibioticGroup
            {
                Name = name,
                Description = $"{name} antibiotic group",
                ParentId = parentId,
            };
        }

        private static Antibiotic Antibiotic(
            string name,
            Guid groupId,
            AwareClassification classification,
            bool softDeleted = false)
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

        /*
         * Seeds a realistic antibiotic catalogue with a three-level beta-lactam
         * hierarchy (beta-lactams > penicillins > aminopenicillins), so the
         * "same group", "sibling sub-group" and "deep common ancestor" business
         * rules can all be exercised.
         */
        private async Task<SeedData> SeedAsync()
        {
            var betaLactams = Group("Beta-lactams");
            var penicillins = Group("Penicillins", betaLactams.Id);
            var aminopenicillins = Group("Aminopenicillins", penicillins.Id);
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var carbapenems = Group("Carbapenems", betaLactams.Id);
            var macrolides = Group("Macrolides");
            var fluoroquinolones = Group("Fluoroquinolones");
            var glycopeptides = Group("Glycopeptides");
            var oxazolidinones = Group("Oxazolidinones");

            var amoxicillin = Antibiotic("Amoxicillin", aminopenicillins.Id, AwareClassification.Access);
            var ampicillin = Antibiotic("Ampicillin", aminopenicillins.Id, AwareClassification.Access);
            var benzylpenicillin = Antibiotic("Benzylpenicillin", penicillins.Id, AwareClassification.Access);
            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var meropenem = Antibiotic("Meropenem", carbapenems.Id, AwareClassification.Reserve);
            var azithromycin = Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch);
            var levofloxacin = Antibiotic("Levofloxacin", fluoroquinolones.Id, AwareClassification.Watch);
            var vancomycin = Antibiotic("Vancomycin", glycopeptides.Id, AwareClassification.Watch);
            var linezolid = Antibiotic("Linezolid", oxazolidinones.Id, AwareClassification.Reserve);
            var telavancin = Antibiotic(
                "Telavancin", glycopeptides.Id, AwareClassification.Watch, softDeleted: true);

            await using var seedContext = new ClinicalDbContext(_options);
            seedContext.AntibioticGroups.AddRange(
                betaLactams,
                penicillins,
                aminopenicillins,
                cephalosporins,
                carbapenems,
                macrolides,
                fluoroquinolones,
                glycopeptides,
                oxazolidinones);
            seedContext.Antibiotics.AddRange(
                amoxicillin,
                ampicillin,
                benzylpenicillin,
                ceftriaxone,
                meropenem,
                azithromycin,
                levofloxacin,
                vancomycin,
                linezolid,
                telavancin);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new SeedData(
                betaLactams,
                penicillins,
                aminopenicillins,
                cephalosporins,
                carbapenems,
                macrolides,
                fluoroquinolones,
                glycopeptides,
                oxazolidinones,
                amoxicillin,
                ampicillin,
                benzylpenicillin,
                ceftriaxone,
                meropenem,
                azithromycin,
                levofloxacin,
                vancomycin,
                linezolid,
                telavancin);
        }

        #endregion

        #region Query helpers

        private static WarnAntibioticCombinationQuery CreateQuery(params Guid[] antibioticIds)
        {
            return new WarnAntibioticCombinationQuery
            {
                AntibioticIds = [.. antibioticIds],
            };
        }

        private static void AssertSuccessWithoutWarning(
            Result<WarnAntibioticCombinationResult> result)
        {
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.False(result.Data.NeedWarning);
        }

        private static void AssertSuccessWithWarning(
            Result<WarnAntibioticCombinationResult> result)
        {
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.True(result.Data.NeedWarning);
        }

        private static void AssertFailure(
            Result<WarnAntibioticCombinationResult> result,
            string expectedStatusCode)
        {
            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal(expectedStatusCode, result.StatusCode);
            Assert.Equal(expectedStatusCode, result.Error.StatusCode);
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task WarnAntibioticCombination_SingleAntibiotic_NoWarning()
        {
            // Boundary: 1 antibiotic is the smallest possible pick. A single
            // antibiotic cannot share a lineage with itself in a combination
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Amoxicillin.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithoutWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_EmptyAntibioticList_NoWarning()
        {
            // Boundary: 0 antibiotics. The validator rejects this before the handler
            // in the real pipeline, but the handler itself must not warn on nothing
            await SeedAsync();
            var query = CreateQuery();

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithoutWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_TwoUnrelatedLineages_NoWarning()
        {
            // Conventional moderate CAP therapy: a third-generation cephalosporin
            // plus a macrolide. They belong to unrelated group lineages
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Ceftriaxone.Id, seed.Azithromycin.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithoutWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_ThreeUnrelatedLineages_NoWarning()
        {
            // Boundary: 3 antibiotics is the largest realistic custom combination.
            // Beta-lactam, macrolide and oxazolidinone lineages are all unrelated,
            // so every pair must be checked before returning no warning
            var seed = await SeedAsync();
            var query = CreateQuery(
                seed.Ceftriaxone.Id, seed.Azithromycin.Id, seed.Linezolid.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithoutWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_SameGroup_NeedWarning()
        {
            // Amoxicillin and ampicillin are both aminopenicillins. Picking two
            // antibiotics from the exact same group must trigger the warning
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Amoxicillin.Id, seed.Ampicillin.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_SiblingGroups_NeedWarning()
        {
            /*
             * Ceftriaxone (cephalosporins) and meropenem (carbapenems) are different
             * groups, but both descend from beta-lactams. Combining two beta-lactam
             * sub-classes is exactly the unusual custom pick the warning exists for
             */
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Ceftriaxone.Id, seed.Meropenem.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_ParentAndChildGroups_NeedWarning()
        {
            /*
             * Benzylpenicillin is a penicillin while amoxicillin is an
             * aminopenicillin, a child of penicillins. A parent group and its
             * descendant share the same lineage
             */
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Benzylpenicillin.Id, seed.Amoxicillin.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_DeepCommonAncestor_NeedWarning()
        {
            /*
             * Amoxicillin (beta-lactams > penicillins > aminopenicillins) and
             * ceftriaxone (beta-lactams > cephalosporins) only meet at the
             * third level of the hierarchy. The lineage traversal must walk the
             * whole ancestor chain, not just the immediate parent group
             */
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Amoxicillin.Id, seed.Ceftriaxone.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_NonAdjacentSharedLineage_NeedWarning()
        {
            // Boundary: 3 antibiotics where the shared lineage is not between the
            // first two sorted entries (azithromycin sits between the beta-lactams).
            // All pairs must be inspected, not only adjacent ones
            var seed = await SeedAsync();
            var query = CreateQuery(
                seed.Ceftriaxone.Id, seed.Azithromycin.Id, seed.Meropenem.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithWarning(result);
        }

        [Fact]
        public async Task WarnAntibioticCombination_ReversedOrder_NeedWarning()
        {
            // The warning must not depend on the order the doctor picked the
            // medicines in. Same sibling beta-lactam pick, reversed order
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Meropenem.Id, seed.Ceftriaxone.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertSuccessWithWarning(result);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task WarnAntibioticCombination_UnknownAntibiotic_Fail()
        {
            // Boundary: 1 of the 2 provided IDs does not exist in the catalogue.
            // The whole request must be rejected instead of warning on partial data
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Amoxicillin.Id, Guid.CreateVersion7());

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
        }

        [Fact]
        public async Task WarnAntibioticCombination_AllAntibioticsUnknown_Fail()
        {
            // Boundary: 0 of the 1 provided IDs resolve to a catalogue antibiotic
            var query = CreateQuery(Guid.CreateVersion7());

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
        }

        [Fact]
        public async Task WarnAntibioticCombination_RetiredAntibiotic_Fail()
        {
            // The antibiotic still exists in the table, but it is soft deleted and
            // hidden by the query filter, so it must be treated as missing
            var seed = await SeedAsync();
            var query = CreateQuery(seed.RetiredAntibiotic.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
        }

        [Fact]
        public async Task WarnAntibioticCombination_DuplicateAntibioticIds_Fail()
        {
            // Boundary: 2 provided entries resolve to 1 catalogue antibiotic. The
            // validator rejects duplicates earlier, and the handler must not mistake
            // the count mismatch for a complete pick either
            var seed = await SeedAsync();
            var query = CreateQuery(seed.Amoxicillin.Id, seed.Amoxicillin.Id);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
        }

        [Fact]
        public async Task WarnAntibioticCombination_ServiceFails_PropagatesError()
        {
            /*
             * The clinical context always contains every antibiotic group loaded
             * from the database, so IsMedicineCombinationValid cannot fail through
             * the real service in normal data. A strict mock is used to verify the
             * handler forwards the upstream error untouched
             */
            var seed = await SeedAsync();
            var upstreamError = new ApplicationError(
                ApplicationStatus.BadRequest,
                "Antibiotic group not found");
            var service = new Mock<IDiagnoseService>(MockBehavior.Strict);
            service
                .Setup(x => x.IsMedicineCombinationValid(
                    It.IsAny<ClinicalContext>(),
                    It.IsAny<IEnumerable<Antibiotic>>()))
                .Returns(Result<bool>.Failure(upstreamError));
            var handler = new WarnAntibioticCombinationHandler(
                _context,
                service.Object,
                new Mock<ILogger<WarnAntibioticCombinationHandler>>().Object);

            var query = CreateQuery(seed.Amoxicillin.Id);

            var result = await handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, upstreamError.StatusCode);

            service.Verify(
                x => x.IsMedicineCombinationValid(
                    It.IsAny<ClinicalContext>(),
                    It.IsAny<IEnumerable<Antibiotic>>()),
                Times.Once);
        }

        #endregion
    }
}
