using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.RiskFactors.UpdateRiskFactor
{
    public class UpdateRiskFactorMapperTest
    {
        private readonly IUpdateMapper<RiskFactor, UpdateRiskFactorCommand> _mapper = new UpdateRiskFactorMapper();

        // A fixed old timestamp makes the "moved forward" assertion deterministic
        private static readonly DateTimeOffset Stale = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static RiskFactor CreateRiskFactor(Guid pathogenId, Guid criterionId)
        {
            return new RiskFactor
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
                UpdatedAt = Stale,
            };
        }

        private static UpdateRiskFactorCommand CreateCommand(RiskFactor model, Guid pathogenId, Guid criterionId) =>
            new() { Id = model.Id, PathogenId = pathogenId, CriterionId = criterionId };

        #region Pathogen and criterion mapping

        [Fact]
        public void MapModel_RepointsBothForeignKeys_Success()
        {
            var model = CreateRiskFactor(Guid.CreateVersion7(), Guid.CreateVersion7());
            var newPathogenId = Guid.CreateVersion7();
            var newCriterionId = Guid.CreateVersion7();

            var result = _mapper.MapModel(model, CreateCommand(model, newPathogenId, newCriterionId));

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(newPathogenId, model.PathogenId);
            Assert.Equal(newCriterionId, model.CriterionId);
            Assert.True(
                model.UpdatedAt > Stale,
                $"UpdatedAt {model.UpdatedAt} should move past the stale value {Stale}");
        }

        [Fact]
        public void MapModel_KeepsIdentityAndSoftDeleteState_Success()
        {
            /*
             * Update rewrites the two foreign keys only: the identity rows are keyed on,
             * the create timestamp and the soft delete state must all survive
             */
            var model = CreateRiskFactor(Guid.CreateVersion7(), Guid.CreateVersion7());
            var originalId = model.Id;
            var originalCreatedAt = model.CreatedAt;

            var result = _mapper.MapModel(
                model, CreateCommand(model, Guid.CreateVersion7(), Guid.CreateVersion7()));

            Assert.True(result.IsSuccess());
            Assert.Equal(originalId, model.Id);
            Assert.Equal(originalCreatedAt, model.CreatedAt);
            Assert.False(model.IsDeleted);
            Assert.Null(model.DeletedAt);
        }

        [Fact]
        public void MapModel_WithDependencies_IgnoresDependencies_Success()
        {
            /*
             * The risk factor only carries the two foreign keys, so the overload that
             * accepts dependencies must produce exactly the same mapping
             */
            var model = CreateRiskFactor(Guid.CreateVersion7(), Guid.CreateVersion7());
            var pathogenId = Guid.CreateVersion7();
            var criterionId = Guid.CreateVersion7();
            var command = CreateCommand(model, pathogenId, criterionId);

            var withoutDependencies = _mapper.MapModel(model, command);
            Assert.True(withoutDependencies.IsSuccess());

            var model2 = CreateRiskFactor(Guid.CreateVersion7(), Guid.CreateVersion7());
            var command2 = CreateCommand(model2, pathogenId, criterionId);
            var withObjectDependencies = _mapper.MapModel(model2, command2, new object());

            Assert.True(withObjectDependencies.IsSuccess());
            Assert.Equal(pathogenId, model2.PathogenId);
            Assert.Equal(criterionId, model2.CriterionId);
            Assert.True(model2.UpdatedAt > Stale);
        }

        #endregion

        /*
         * Fail path: UpdateRiskFactorMapper.MapModel always returns Success, so the
         * mapper failure branch of the handler cannot be reached in reality and is
         * skipped rather than forced with a stub mapper
         */
    }
}
