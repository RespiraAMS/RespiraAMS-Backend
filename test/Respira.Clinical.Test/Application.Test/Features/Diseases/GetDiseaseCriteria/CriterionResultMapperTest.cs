using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Range = Domain.Models.Range;

namespace Application.Test.Features.Diseases.GetDiseaseCriteria;

public class CriterionResultMapperTest
{
    private readonly CriterionResultMapper _mapper = new();

    # region Happy path

    [Fact]
    public void ToResult_BooleanCriterion_MapsAllFields()
    {
        var model = new BooleanCriterion { Name = "Prior history of COPD" };

        var result = _mapper.ToResult(model);

        Assert.Equal(model.Id, result.Id);
        Assert.Equal("Prior history of COPD", result.Name);
        Assert.Equal(CriterionType.Boolean, result.Type);
        // Boolean criteria carry no numeric value
        Assert.Null(result.Value);
    }

    [Fact]
    public void ToResult_NumericCriterion_MapsValue()
    {
        var model = new NumericCriterion
        {
            Name = "Respiratory rate",
            Value = new Range
            {
                Min = 30,
                IsMinExclusive = false,
                Max = 100,
                IsMaxExclusive = false,
                Unit = "breaths/min",
            },
        };

        var result = _mapper.ToResult(model);

        Assert.Equal(model.Id, result.Id);
        Assert.Equal("Respiratory rate", result.Name);
        Assert.Equal(CriterionType.Numeric, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(30, result.Value.Min);
        Assert.Equal(100, result.Value.Max);
        Assert.Equal("breaths/min", result.Value.Unit);
    }

    # endregion
}
