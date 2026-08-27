using Application.Features.Diagnose.EmpiricalDiagnose;

namespace Application.Test.Features.Diagnose.EmpiricalDiagnose;

public class EmpiricalDiagnoseValidatorTest
{
    private readonly EmpiricalDiagnoseValidator _validator = new();

    private static EmpiricalDiagnoseQuery ValidQuery()
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
    public void EmpiricalDiagnose_ValidQuery_Passes()
    {
        var result = _validator.Validate(ValidQuery());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmpiricalDiagnose_MissingDiseaseId_Fail()
    {
        var query = ValidQuery();
        query.DiseaseId = Guid.Empty;

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DiseaseId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EmpiricalDiagnose_InvalidWeight_Fail(decimal weight)
    {
        var query = ValidQuery();
        query.Weight = weight;

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Weight");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EmpiricalDiagnose_InvalidHeight_Fail(decimal height)
    {
        var query = ValidQuery();
        query.Height = height;

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Height");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EmpiricalDiagnose_InvalidSerumCreatine_Fail(decimal value)
    {
        var query = ValidQuery();
        query.SerumCreatine = value;

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SerumCreatine");
    }

    [Fact]
    public void EmpiricalDiagnose_FutureDateOfBirth_Fail()
    {
        var query = ValidQuery();
        query.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DateOfBirth");
    }

    [Fact]
    public void EmpiricalDiagnose_NullUrea_Passes()
    {
        var query = ValidQuery();
        query.Urea = null;

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EmpiricalDiagnose_InvalidUrea_Fail(decimal urea)
    {
        var query = ValidQuery();
        query.Urea = urea;

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Urea");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EmpiricalDiagnose_InvalidRespiratory_Fail(int value)
    {
        var query = ValidQuery();
        query.Respiratory = value;

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Respiratory");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EmpiricalDiagnose_InvalidSystolic_Fail(decimal value)
    {
        var query = ValidQuery();
        query.SystolicBloodPressure = value;

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SystolicBloodPressure");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EmpiricalDiagnose_InvalidDiastolic_Fail(decimal value)
    {
        var query = ValidQuery();
        query.DiastolicBloodPressure = value;

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DiastolicBloodPressure");
    }

    [Fact]
    public void EmpiricalDiagnose_EmptyCriterionIdInList_Fail()
    {
        var query = ValidQuery();
        query.IcuHospitalizeCriteria = [Guid.Empty];

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("IcuHospitalizeCriteria"));
    }

    [Fact]
    public void EmpiricalDiagnose_EmptyResistanceFactorIdInList_Fail()
    {
        var query = ValidQuery();
        query.ResistanceRiskFactors = [Guid.Empty];

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("ResistanceRiskFactors"));
    }

    [Fact]
    public void EmpiricalDiagnose_EmptyOtherCriterionIdInList_Fail()
    {
        var query = ValidQuery();
        query.OtherCriteria = [Guid.Empty];

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("OtherCriteria"));
    }
}
