using Application.Contracts.Mappers;
using Application.Features.Causes.CreateCause;
using Domain.Enums;
using Domain.Models;

namespace Application.Test.Features.Causes.CreateCause;

public class CreateCauseMapperTest
{
    private readonly ICreateMapper<Cause, CreateCauseCommand> _mapper = new CreateCauseMapper();

    # region Happy path

    [Fact]
    public void ToModel_Success()
    {
        var diseaseId = Guid.CreateVersion7();
        var pathogenId = Guid.CreateVersion7();
        var command = new CreateCauseCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
        };

        var model = _mapper.ToModel(command);

        // Base generates the ID so the handler can return it right after saving
        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.Equal(diseaseId, model.DiseaseId);
        Assert.Equal(pathogenId, model.PathogenId);
        Assert.Equal(Severity.Severe, model.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, model.TreatmentSite);
    }

    # endregion
}
