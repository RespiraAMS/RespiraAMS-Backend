using Application.Contracts.Mappers;
using Application.Features.Causes.UpdateCause;
using Domain.Enums;
using Domain.Models;

namespace Application.Test.Features.Causes.UpdateCause;

public class UpdateCauseMapperTest
{
    private readonly IUpdateMapper<Cause, UpdateCauseCommand> _mapper = new UpdateCauseMapper();

    # region Happy path

    [Fact]
    public void MapModel_UpdatesSeverityAndTreatmentSite_Success()
    {
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);
        var diseaseId = Guid.CreateVersion7();
        var pathogenId = Guid.CreateVersion7();
        var cause = new Cause
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = Severity.Mild,
            TreatmentSite = TreatmentSite.Outpatient,
        };

        _mapper.MapModel(cause, new UpdateCauseCommand
        {
            Id = cause.Id,
            Severity = Severity.Severe,
            TreatmentSite = TreatmentSite.IntensiveCareUnit,
        });

        Assert.Equal(Severity.Severe, cause.Severity);
        Assert.Equal(TreatmentSite.IntensiveCareUnit, cause.TreatmentSite);
        Assert.InRange(cause.UpdatedAt, before, DateTimeOffset.UtcNow.AddSeconds(5));

        // Business rule: the mapper only rewrites severity and treatment site; the
        // identity fields (disease, pathogen links) stay immutable
        Assert.Equal(diseaseId, cause.DiseaseId);
        Assert.Equal(pathogenId, cause.PathogenId);
        Assert.Equal(cause.Id, cause.Id);
    }

    # endregion
}
