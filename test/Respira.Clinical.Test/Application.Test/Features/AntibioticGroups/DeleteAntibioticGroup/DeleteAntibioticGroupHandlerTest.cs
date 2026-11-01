using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.AntibioticGroups.DeleteAntibioticGroup;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.AntibioticGroups.DeleteAntibioticGroup
{
    public class DeleteAntibioticGroupHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly DeleteAntibioticGroupHandler _handler;
        private readonly IDbContext _context;

        public DeleteAntibioticGroupHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>().UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<DeleteAntibioticGroupHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Antibiotics reference groups through an FK, so delete them first.
            // IgnoreQueryFilters is needed because soft-deleted rows are hidden by the
            // query filter but still occupy the table. Groups reference each other
            // through ParentId, so sub-groups go before their parents
            await _context.Antibiotics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .Where(x => x.ParentId != null)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        /*
         * Reference data is written through its own context, so the handler context starts
         * clean just like a real HTTP request.
         */
        private async Task SeedGroupsAsync(params AntibioticGroup[] groups)
        {
            await using var seedContext = new ClinicalDbContext(_options);
            seedContext.AntibioticGroups.AddRange(groups);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        private async Task SeedAntibioticsAsync(params Antibiotic[] antibiotics)
        {
            await using var seedContext = new ClinicalDbContext(_options);
            seedContext.Antibiotics.AddRange(antibiotics);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        private static AntibioticGroup Group(
            string name, Guid? parentId = null, bool softDeleted = false)
        {
            return new AntibioticGroup
            {
                Name = name,
                Description = $"{name} antibiotic group",
                ParentId = parentId,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            };
        }

        private static Antibiotic Antibiotic(
            string name, Guid antibioticGroupId, AwareClassification classification,
            bool softDeleted = false)
        {
            return new Antibiotic
            {
                Name = name,
                AntibioticGroupId = antibioticGroupId,
                Classification = classification,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow.AddDays(-1) : null,
            };
        }

        private async Task<AntibioticGroup> LoadGroupAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.AntibioticGroups.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<Antibiotic> LoadAntibioticAsync(Guid id)
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return await freshContext.Antibiotics.IgnoreQueryFilters()
                .SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        private async Task<(int DeletedGroups, int DeletedAntibiotics)> CountSoftDeletedAsync()
        {
            await using var freshContext = new ClinicalDbContext(_options);
            return (
                await freshContext.AntibioticGroups.IgnoreQueryFilters()
                    .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken),
                await freshContext.Antibiotics.IgnoreQueryFilters()
                    .CountAsync(x => x.IsDeleted, TestContext.Current.CancellationToken));
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task DeleteAntibioticGroup_NoReferences_Success()
        {
            // A leaf group that no antibiotic and no other group points at
            var target = Group("Fluoroquinolones");
            var control = Group("Macrolides");
            await SeedGroupsAsync(target, control);

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(target.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            // Business rule: the group is soft deleted, never removed
            var deleted = await LoadGroupAsync(target.Id);
            Assert.True(deleted.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);

            // The default query filter must now hide the deleted group
            await using var freshContext = new ClinicalDbContext(_options);
            Assert.False(await freshContext.AntibioticGroups
                .AnyAsync(x => x.Id == target.Id, TestContext.Current.CancellationToken));

            // The unrelated control group stays live
            var untouched = await LoadGroupAsync(control.Id);
            Assert.False(untouched.IsDeleted);
            Assert.Null(untouched.DeletedAt);
        }

        [Fact]
        public async Task DeleteAntibioticGroup_SubGroupWithoutReferences_Success()
        {
            /*
             * Boundary: a group that itself has a parent is only a reference holder, not a
             * referenced group. Deleting it must be allowed because nothing points at it
             */
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            await SeedGroupsAsync(betaLactams, cephalosporins);

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(cephalosporins.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);

            Assert.True((await LoadGroupAsync(cephalosporins.Id)).IsDeleted);

            // The parent group must not be cascaded
            var deletedParent = await LoadGroupAsync(betaLactams.Id);
            Assert.False(deletedParent.IsDeleted);
            Assert.Null(deletedParent.DeletedAt);
        }

        [Fact]
        public async Task DeleteAntibioticGroup_OnlySoftDeletedReferences_Success()
        {
            /*
             * Soft-deleted child groups and antibiotics are hidden by the query filter, so
             * they are no longer part of the clinical knowledge base and must not forbid
             * the delete. The rejected rows keep their original deletion timestamp
             */
            var target = Group("Beta-lactams");
            var control = Group("Macrolides");
            var deletedSubGroup = Group("Cephalosporins", target.Id, softDeleted: true);

            await SeedGroupsAsync(target, control, deletedSubGroup);
            var deletedAntibiotic = Antibiotic(
                "Ceftriaxone", target.Id, AwareClassification.Watch, softDeleted: true);
            await SeedAntibioticsAsync(deletedAntibiotic);

            var subGroupDeletedAt = (await LoadGroupAsync(deletedSubGroup.Id)).DeletedAt;
            var antibioticDeletedAt = (await LoadAntibioticAsync(deletedAntibiotic.Id)).DeletedAt;

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(target.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);
            Assert.True((await LoadGroupAsync(target.Id)).IsDeleted);

            // The failed-to-see references were already deleted and stay exactly as they were
            var keptSubGroup = await LoadGroupAsync(deletedSubGroup.Id);
            Assert.True(keptSubGroup.IsDeleted);
            Assert.Equal(subGroupDeletedAt, keptSubGroup.DeletedAt);

            var keptAntibiotic = await LoadAntibioticAsync(deletedAntibiotic.Id);
            Assert.True(keptAntibiotic.IsDeleted);
            Assert.Equal(antibioticDeletedAt, keptAntibiotic.DeletedAt);

            Assert.False((await LoadGroupAsync(control.Id)).IsDeleted);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task DeleteAntibioticGroup_ReferencedBySubGroup_Fail()
        {
            // Business rule: a group with live sub-groups is still in use
            var target = Group("Beta-lactams");
            var subGroup = Group("Carbapenems", target.Id);
            await SeedGroupsAsync(target, subGroup);

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(target.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            // Nothing may be soft deleted when the rule is violated
            Assert.Equal((0, 0), await CountSoftDeletedAsync());
            Assert.False((await LoadGroupAsync(target.Id)).IsDeleted);
            Assert.False((await LoadGroupAsync(subGroup.Id)).IsDeleted);
        }

        [Fact]
        public async Task DeleteAntibioticGroup_ReferencedByAntibiotics_Fail()
        {
            /*
             * Boundary: a group holding antibiotics is in use. Two antibiotics are seeded
             * so the existential check is exercised beyond the single-row boundary
             */
            var target = Group("Beta-lactams");
            var control = Group("Macrolides");
            await SeedGroupsAsync(target, control);

            var amoxicillin = Antibiotic(
                "Amoxicillin", target.Id, AwareClassification.Access);
            var meropenem = Antibiotic(
                "Meropenem", target.Id, AwareClassification.Watch);
            var azithromycin = Antibiotic(
                "Azithromycin", control.Id, AwareClassification.Watch);
            await SeedAntibioticsAsync(amoxicillin, meropenem, azithromycin);

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(target.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            Assert.Equal((0, 0), await CountSoftDeletedAsync());
            Assert.False((await LoadAntibioticAsync(amoxicillin.Id)).IsDeleted);
            Assert.False((await LoadAntibioticAsync(meropenem.Id)).IsDeleted);
            Assert.False((await LoadAntibioticAsync(azithromycin.Id)).IsDeleted);
        }

        [Fact]
        public async Task DeleteAntibioticGroup_ReferencedByBothGroupAndAntibiotic_Fail()
        {
            // Upper boundary of the rule: both kinds of reference exist at once
            var target = Group("Beta-lactams");
            var subGroup = Group("Cephalosporins", target.Id);
            await SeedGroupsAsync(target, subGroup);
            var ceftriaxone = Antibiotic(
                "Ceftriaxone", target.Id, AwareClassification.Watch);
            await SeedAntibioticsAsync(ceftriaxone);

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(target.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);

            Assert.Equal((0, 0), await CountSoftDeletedAsync());
            Assert.False((await LoadGroupAsync(target.Id)).IsDeleted);
            Assert.False((await LoadGroupAsync(subGroup.Id)).IsDeleted);
            Assert.False((await LoadAntibioticAsync(ceftriaxone.Id)).IsDeleted);
        }

        [Fact]
        public async Task DeleteAntibioticGroup_UnknownId_Fail()
        {
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(unknownId),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            // Nothing must be soft-deleted when the target does not exist
            Assert.Equal((0, 0), await CountSoftDeletedAsync());
        }

        [Fact]
        public async Task DeleteAntibioticGroup_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored group
            await SeedGroupsAsync(Group("Beta-lactams"));

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(Guid.Empty),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            Assert.Equal((0, 0), await CountSoftDeletedAsync());
        }

        [Fact]
        public async Task DeleteAntibioticGroup_AlreadyDeleted_Fail()
        {
            // The !IsDeleted query filter hides soft-deleted rows, so a retried delete is
            // reported as a missing group and must not move the deletion timestamp
            var target = Group("Beta-lactams", softDeleted: true);
            await SeedGroupsAsync(target);

            var originalDeletedAt = (await LoadGroupAsync(target.Id)).DeletedAt;

            var result = await _handler.HandleAsync(
                new DeleteAntibioticGroupCommand(target.Id),
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);

            var unchanged = await LoadGroupAsync(target.Id);
            Assert.True(unchanged.IsDeleted);
            Assert.Equal(originalDeletedAt, unchanged.DeletedAt);
        }

        #endregion
    }
}
