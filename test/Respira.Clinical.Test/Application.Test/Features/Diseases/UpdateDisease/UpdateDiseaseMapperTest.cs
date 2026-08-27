using Application.Contracts.Mappers;
using Application.Features.Diseases.UpdateDisease;
using Domain.Models;

namespace Application.Test.Features.Diseases.UpdateDisease;

public class UpdateDiseaseMapperTest
{
    private readonly IUpdateMapper<Disease, UpdateDiseaseCommand> _mapper = new UpdateDiseaseMapper();

    # region Happy path

    [Fact]
    public void MapModel_MapsAllScalarFields()
    {
        var model = new Disease
        {
            Name = "Community-Acquired Pneumonia",
            Description = "Infection of the lung parenchyma acquired outside of a healthcare setting",
            IcuScoreThreshold = 3,
        };
        var updatedAtBefore = model.UpdatedAt;

        var command = new UpdateDiseaseCommand
        {
            Id = model.Id,
            Name = "Hospital-Acquired Pneumonia",
            Description = "Pneumonia developing more than 48 hours after hospital admission",
            IcuScoreThreshold = 5,
        };

        _mapper.MapModel(model, command);

        Assert.Equal(command.Name, model.Name);
        Assert.Equal(command.Description, model.Description);
        Assert.Equal(command.IcuScoreThreshold, model.IcuScoreThreshold);

        // Update timestamp must be refreshed on every mapping
        Assert.NotEqual(updatedAtBefore, model.UpdatedAt);

        // The mapper must never change the primary key
        Assert.Equal(command.Id, model.Id);
    }

    [Fact]
    public void MapModel_ZeroThreshold_IsStillMapped()
    {
        // The mapper is a mechanical projection: it applies whatever threshold is supplied.
        // The > 0 rule is enforced by the validator, not the mapper.
        var model = new Disease
        {
            Name = "Sepsis",
            Description = "Life-threatening organ dysfunction due to dysregulated host response",
            IcuScoreThreshold = 4,
        };

        var command = new UpdateDiseaseCommand
        {
            Id = model.Id,
            Name = "Severe Sepsis",
            Description = "Sepsis with refractory hypotension requiring vasopressors",
            IcuScoreThreshold = 0,
        };

        _mapper.MapModel(model, command);

        Assert.Equal(0, model.IcuScoreThreshold);
        Assert.Equal("Severe Sepsis", model.Name);
    }

    # endregion
}
