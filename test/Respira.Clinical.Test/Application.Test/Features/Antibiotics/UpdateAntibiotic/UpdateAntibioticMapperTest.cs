using Application.Contracts.Mappers;
using Application.Features.Antibiotics.UpdateAntibiotic;
using Domain.Enums;
using Domain.Models;

namespace Application.Test.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticMapperTest
{
    private readonly IUpdateMapper<Antibiotic, UpdateAntibioticCommand> _mapper =
        new UpdateAntibioticMapper();

    # region Happy path

    [Fact]
    public void MapModel_Success()
    {
        var oldGroupId = Guid.CreateVersion7();
        var model = new Antibiotic
        {
            Name = "Amoxicillin",
            AntibioticGroupId = oldGroupId,
            Classification = AwareClassification.Access,
        };
        var updatedAtBeforeMapping = model.UpdatedAt;
        var newGroupId = Guid.CreateVersion7();

        var command = new UpdateAntibioticCommand
        {
            Id = model.Id,
            Name = "Co-amoxiclav",
            AntibioticGroupId = newGroupId,
            Classification = AwareClassification.AccessWatch,
        };

        _mapper.MapModel(model, command);

        Assert.Equal("Co-amoxiclav", model.Name);
        Assert.Equal(newGroupId, model.AntibioticGroupId);
        Assert.Equal(AwareClassification.AccessWatch, model.Classification);
        Assert.NotEqual(updatedAtBeforeMapping, model.UpdatedAt);
    }

    # endregion
}
