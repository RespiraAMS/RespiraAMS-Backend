using Application.Contracts.Mappers;
using Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Domain.Models;

namespace Application.Test.Features.AntibioticGroups.UpdateAntibioticGroup;

public class UpdateAntibioticGroupMapperTest
{
    private readonly IUpdateMapper<AntibioticGroup, UpdateAntibioticGroupCommand> _mapper =
        new UpdateAntibioticGroupMapper();

    # region Happy path

    [Fact]
    public void MapModel_WithParent_Success()
    {
        var model = new AntibioticGroup
        {
            Name = "Beta-lactams",
            Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
            ParentId = null,
        };
        var updatedAtBeforeMapping = model.UpdatedAt;
        var parentId = Guid.CreateVersion7();

        var command = new UpdateAntibioticGroupCommand
        {
            Id = model.Id,
            Name = "Penicillins",
            Description = "Beta-lactam antibiotics active against gram-positive organisms",
            ParentId = parentId,
        };

        _mapper.MapModel(model, command);

        Assert.Equal(command.Name, model.Name);
        Assert.Equal(command.Description, model.Description);
        Assert.Equal(parentId, model.ParentId);
        Assert.NotEqual(updatedAtBeforeMapping, model.UpdatedAt);
    }

    [Fact]
    public void MapModel_RootGroup_ClearsParentId()
    {
        var parentId = Guid.CreateVersion7();
        var model = new AntibioticGroup
        {
            Name = "Penicillins",
            Description = "Subgroup of beta-lactam antibiotics",
            ParentId = parentId,
        };

        var command = new UpdateAntibioticGroupCommand
        {
            Id = model.Id,
            Name = "Penicillins",
            Description = "Promoted to a standalone root group",
            ParentId = null,
        };

        _mapper.MapModel(model, command);

        Assert.Equal(command.Name, model.Name);
        Assert.Equal(command.Description, model.Description);
        Assert.Null(model.ParentId);
    }

    # endregion
}
