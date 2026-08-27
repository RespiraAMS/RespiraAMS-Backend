using Application.Features.Diagnose.EmpiricalDiagnose;

namespace Application.Test.Features.Diagnose.EmpiricalDiagnose;

public class EmpiricalDiagnoseClinicalPictureMapperTest
{
    private readonly EmpiricalDiagnoseClinicalPictureMapper _mapper = new();

    private static EmpiricalDiagnoseQuery QueryWith(
        bool confusion, decimal? urea, int respiratory, decimal systolic, decimal diastolic,
        List<Guid> icu, List<Guid> rrf, List<Guid> other)
    {
        return new EmpiricalDiagnoseQuery
        {
            DiseaseId = Guid.CreateVersion7(),
            DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-50),
            IsMale = true,
            Weight = 70m,
            Height = 1.7m,
            SerumCreatine = 1.0m,
            Confusion = confusion,
            Urea = urea,
            Respiratory = respiratory,
            SystolicBloodPressure = systolic,
            DiastolicBloodPressure = diastolic,
            IcuHospitalizeCriteria = icu,
            ResistanceRiskFactors = rrf,
            OtherCriteria = other,
        };
    }

    [Fact]
    public void Map_MapsFullClinicalPicture_Success()
    {
        var icu = new List<Guid> { Guid.CreateVersion7() };
        var rrf = new List<Guid> { Guid.CreateVersion7() };
        var other = new List<Guid> { Guid.CreateVersion7() };
        var query = QueryWith(
            confusion: true, urea: 8m, respiratory: 32, systolic: 80m, diastolic: 50m, icu, rrf, other);

        var result = _mapper.Map(query);

        Assert.True(result.Confusion);
        Assert.Equal(8m, result.Urea);
        Assert.Equal(32, result.Respiratory);
        Assert.Equal(80m, result.SystolicBloodPressure);
        Assert.Equal(50m, result.DiastolicBloodPressure);
        Assert.Equal(icu, result.IcuHospitalizeCriteria);
        Assert.Equal(rrf, result.ResistanceRiskFactors);
        Assert.Equal(other, result.OtherCriteria);
    }

    [Fact]
    public void Map_MapsEmptyClinicalPicture_Success()
    {
        var query = QueryWith(
            confusion: false, urea: null, respiratory: 18, systolic: 120m, diastolic: 80m,
            [], [], []);

        var result = _mapper.Map(query);

        Assert.False(result.Confusion);
        Assert.Null(result.Urea);
        Assert.Equal(18, result.Respiratory);
        Assert.Equal(120m, result.SystolicBloodPressure);
        Assert.Equal(80m, result.DiastolicBloodPressure);
        Assert.Empty(result.IcuHospitalizeCriteria);
        Assert.Empty(result.ResistanceRiskFactors);
        Assert.Empty(result.OtherCriteria);
    }
}
