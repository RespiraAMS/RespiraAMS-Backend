using Application.Features.Diagnose.EmpiricalDiagnose;

namespace Application.Test.Features.Diagnose.EmpiricalDiagnose;

public class EmpiricalDiagnosePatientInfoMapperTest
{
    private readonly EmpiricalDiagnosePatientInfoMapper _mapper = new();

    private static EmpiricalDiagnoseQuery Base()
    {
        return new EmpiricalDiagnoseQuery
        {
            DiseaseId = Guid.CreateVersion7(),
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-50),
            IsMale = true,
            Weight = 70m,
            Height = 1.7m,
            SerumCreatine = 1.0m,
            Confusion = false,
            Urea = 5m,
            Respiratory = 20,
            SystolicBloodPressure = 120m,
            DiastolicBloodPressure = 80m,
            IcuHospitalizeCriteria = [],
            ResistanceRiskFactors = [],
            OtherCriteria = [],
        };
    }

    [Fact]
    public void Map_MapsPatientInfo_Success()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = Base();
        query.DateOfBirth = today.AddYears(-50);

        var result = _mapper.Map(query);

        Assert.Equal(70m, result.Weight);
        Assert.Equal(1.7m, result.Height);
        Assert.Equal(1.0m, result.SerumCreatine);
        Assert.True(result.IsMale);
        Assert.Equal(today.AddYears(-50), result.DateOfBirth);
    }

    [Fact]
    public void Map_FemaleAndBoundary_Success()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = Base();
        query.DateOfBirth = today.AddYears(-20);
        query.IsMale = false;
        query.Weight = 0m;
        query.Height = 0m;
        query.SerumCreatine = 0m;

        var result = _mapper.Map(query);

        Assert.Equal(0m, result.Weight);
        Assert.Equal(0m, result.Height);
        Assert.Equal(0m, result.SerumCreatine);
        Assert.False(result.IsMale);
        Assert.Equal(today.AddYears(-20), result.DateOfBirth);
    }
}
