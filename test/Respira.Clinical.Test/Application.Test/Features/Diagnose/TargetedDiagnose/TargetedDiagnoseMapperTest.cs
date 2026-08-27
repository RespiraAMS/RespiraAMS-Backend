using Application.Features.Diagnose.TargetedDiagnose;
using Domain.Services.Dtos;

namespace Application.Test.Features.Diagnose.TargetedDiagnose;

public class TargetedDiagnoseMapperTest
{
    private readonly TargetedDiagnoseMapper _mapper = new();

    # region Happy path

    [Fact]
    public void Map_TransfersAllPatientFields()
    {
        var query = new TargetedDiagnoseQuery
        {
            PathogenId = Guid.CreateVersion7(),
            DateOfBirth = new DateOnly(1976, 8, 27),
            IsMale = false,
            Weight = 62.5m,
            Height = 1.68m,
            SerumCreatine = 0.9m,
        };

        PatientInfo result = _mapper.Map(query);

        Assert.Equal(query.DateOfBirth, result.DateOfBirth);
        Assert.Equal(query.IsMale, result.IsMale);
        Assert.Equal(query.Weight, result.Weight);
        Assert.Equal(query.Height, result.Height);
        Assert.Equal(query.SerumCreatine, result.SerumCreatine);
    }

    # endregion
}
