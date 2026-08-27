using Application.Contracts.Mappers;
using Application.Features.Antibiograms.CreateAntibiogram;
using Domain.Enums;

namespace Application.Test.Features.Antibiogram.CreateAntibiogram;

using Antibiogram = Domain.Models.Antibiogram;

public class CreateAntibiogramMapperTest
{
    private readonly ICreateMapper<Antibiogram, CreateAntibiogramCommand> _mapper = new CreateAntibiogramMapper();

    # region Happy path

    [Theory]
    [InlineData(MinimumInhibitoryConcentration.Susceptible)]
    [InlineData(MinimumInhibitoryConcentration.Intermediate)]
    [InlineData(MinimumInhibitoryConcentration.Resistance)]
    public void ToModel_Success(MinimumInhibitoryConcentration micLevel)
    {
        var pathogenId = Guid.CreateVersion7();
        var command = new CreateAntibiogramCommand
        {
            PathogenId = pathogenId,
            MicLevel = micLevel,
            MicIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
            FirstPriorityMedicineIds = [Guid.CreateVersion7()],
            SecondPriorityMedicineIds = [],
        };

        var model = _mapper.ToModel(command);

        // Base generates the ID so the handler can return it right after saving
        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.Equal(pathogenId, model.PathogenId);
        Assert.Equal(micLevel, model.MicLevel);

        /*
         * The mapper only carries scalar fields: relation lists stay empty here and
         * are attached by the handler through ID stubs
         */
        Assert.Empty(model.Mics);
        Assert.Empty(model.FirstPriorityMedicines);
        Assert.Empty(model.SecondPriorityMedicines);
    }

    # endregion
}
