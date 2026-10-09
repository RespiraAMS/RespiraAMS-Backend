using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.AntibioticGroups.CreateAntibioticGroup;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.AntibioticGroups.CreateAntibioticGroup
{
    public class CreateAntibioticGroupMapperTest
    {
        private readonly ICreateMapper<CreateAntibioticGroupCommand, AntibioticGroup> _mapper =
            new CreateAntibioticGroupMapper();

        #region Happy path

        [Fact]
        public void ToModel_WithParent_Success()
        {
            var parentId = Guid.CreateVersion7();
            var command = new CreateAntibioticGroupCommand
            {
                Name = "Penicillins",
                Description = "Beta-lactam antibiotics active against gram-positive organisms",
                ParentId = parentId,
            };

            var result = _mapper.ToModel(command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = result.Data;

            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Equal(parentId, model.ParentId);
            // Base generates the ID so the handler can return it right after saving
            Assert.NotEqual(Guid.Empty, model.Id);
        }

        [Fact]
        public void ToModel_RootGroup_KeepsNullParentId()
        {
            var command = new CreateAntibioticGroupCommand
            {
                Name = "Beta-lactams",
                Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
                ParentId = null,
            };

            var result = _mapper.ToModel(command);
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = result.Data;
            Assert.Equal(command.Name, model.Name);
            Assert.Equal(command.Description, model.Description);
            Assert.Null(model.ParentId);
        }

        #endregion
    }
}
