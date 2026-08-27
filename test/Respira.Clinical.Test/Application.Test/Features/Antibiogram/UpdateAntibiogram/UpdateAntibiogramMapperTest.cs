using Application.Contracts.Mappers;
using Application.Features.Antibiograms.UpdateAntibiogram;
using Domain.Enums;

namespace Application.Test.Features.Antibiogram.UpdateAntibiogram;

using Antibiogram = Domain.Models.Antibiogram;

public class UpdateAntibiogramMapperTest
{
    private readonly IUpdateMapper<Antibiogram, UpdateAntibiogramCommand> _mapper =
        new UpdateAntibiogramMapper();

    # region Happy path

    [Fact]
    public void MapModel_UpdatesMicLevelAndTimestamp_Success()
    {
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);
        var pathogenId = Guid.CreateVersion7();
        var model = new Antibiogram
        {
            PathogenId = pathogenId,
            MicLevel = MinimumInhibitoryConcentration.Susceptible,
        };

        _mapper.MapModel(model, new UpdateAntibiogramCommand
        {
            Id = model.Id,
            MicLevel = MinimumInhibitoryConcentration.Resistance,
            MicIds = [Guid.CreateVersion7()],
            FirstPriorityMedicineIds = [Guid.CreateVersion7()],
            SecondPriorityMedicineIds = [],
        });

        Assert.Equal(MinimumInhibitoryConcentration.Resistance, model.MicLevel);
        Assert.Empty(model.MicIds);
        Assert.Empty(model.FirstPriorityMedicineIds);
        Assert.Empty(model.SecondPriorityMedicineIds);
        Assert.InRange(model.UpdatedAt, before, DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void MapModel_NeverChangesPathogen_Success()
    {
        // Business rule: the pathogen of an antibiogram cannot be changed by an update
        var pathogenId = Guid.CreateVersion7();
        var model = new Antibiogram
        {
            PathogenId = pathogenId,
            MicLevel = MinimumInhibitoryConcentration.Intermediate,
        };

        _mapper.MapModel(model, new UpdateAntibiogramCommand
        {
            Id = model.Id,
            MicLevel = MinimumInhibitoryConcentration.Susceptible,
            MicIds = [Guid.CreateVersion7()],
            FirstPriorityMedicineIds = [Guid.CreateVersion7()],
            SecondPriorityMedicineIds = [Guid.CreateVersion7()],
        });

        Assert.Equal(pathogenId, model.PathogenId);
    }

    # endregion
}
