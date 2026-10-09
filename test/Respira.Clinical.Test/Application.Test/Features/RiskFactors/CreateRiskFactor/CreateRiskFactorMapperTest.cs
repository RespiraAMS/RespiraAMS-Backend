using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.RiskFactors.CreateRiskFactor;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.RiskFactors.CreateRiskFactor
{
    public class CreateRiskFactorMapperTest
    {
        private readonly ICreateMapper<CreateRiskFactorCommand, RiskFactor> _mapper = new CreateRiskFactorMapper();

        #region Happy path

        [Fact]
        public void ToModel_Success()
        {
            var pathogenId = Guid.CreateVersion7();
            var criterionId = Guid.CreateVersion7();
            var command = new CreateRiskFactorCommand
            {
                PathogenId = pathogenId,
                CriterionId = criterionId,
            };

            var result = _mapper.ToModel(command);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = result.Data;

            Assert.Equal(pathogenId, model.PathogenId);
            Assert.Equal(criterionId, model.CriterionId);
            // Base generates the ID so the handler can return it right after saving
            Assert.NotEqual(Guid.Empty, model.Id);

            // A newly mapped risk factor is always live, never soft deleted
            Assert.False(model.IsDeleted);
            Assert.Null(model.DeletedAt);
        }

        [Fact]
        public void ToModel_WithDependencies_IgnoresDependencies_Success()
        {
            /*
             * The risk factor only carries the two foreign keys, so the overload that
             * accepts dependencies must produce exactly the same model
             */
            var command = new CreateRiskFactorCommand
            {
                PathogenId = Guid.CreateVersion7(),
                CriterionId = Guid.CreateVersion7(),
            };

            var withoutDependencies = _mapper.ToModel(command);
            var withNullDependencies = _mapper.ToModel(command, null);
            var withObjectDependencies = _mapper.ToModel(command, new object());

            Assert.True(withoutDependencies.IsSuccess());
            Assert.True(withNullDependencies.IsSuccess());
            Assert.True(withObjectDependencies.IsSuccess());
            Assert.NotNull(withoutDependencies.Data);
            Assert.NotNull(withNullDependencies.Data);
            Assert.NotNull(withObjectDependencies.Data);

            Assert.Equal(withoutDependencies.Data.PathogenId, withNullDependencies.Data.PathogenId);
            Assert.Equal(withoutDependencies.Data.CriterionId, withNullDependencies.Data.CriterionId);
            Assert.Equal(withoutDependencies.Data.PathogenId, withObjectDependencies.Data.PathogenId);
            Assert.Equal(withoutDependencies.Data.CriterionId, withObjectDependencies.Data.CriterionId);

            // Every call mints its own entity, so one command never shares a row
            Assert.NotEqual(withoutDependencies.Data.Id, withNullDependencies.Data.Id);
        }

        [Fact]
        public void ToModel_DoesNotValidateIds_Success()
        {
            /*
             * The mapper is a pure projector: it copies the ids it is given. Existence
             * and NotEmpty are the responsibility of the handler and the validator, so
             * an empty id is mapped through untouched instead of being rejected here
             */
            var command = new CreateRiskFactorCommand
            {
                PathogenId = Guid.Empty,
                CriterionId = Guid.Empty,
            };

            var result = _mapper.ToModel(command);
            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(Guid.Empty, result.Data.PathogenId);
            Assert.Equal(Guid.Empty, result.Data.CriterionId);
        }

        #endregion

        /*
         * Fail path: CreateRiskFactorMapper.ToModel always returns Success, so the
         * mapper failure branch of the handler cannot be reached in reality and is
         * skipped rather than forced with a stub mapper
         */
    }
}
