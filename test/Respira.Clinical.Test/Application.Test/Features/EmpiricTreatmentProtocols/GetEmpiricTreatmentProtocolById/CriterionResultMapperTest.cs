using Application.Contracts.Mappers;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Range = Domain.Models.Range;

namespace Application.Test.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

public class CriterionResultMapperTest
{
    private readonly IResultMapper<Criterion, CriterionItem> _mapper = new CriterionResultMapper();

    # region Happy path

    [Fact]
    public void ToResult_BooleanCriterion_MapsIdNameAndType()
    {
        var model = new BooleanCriterion { Name = "Prior history of COPD" };

        var result = _mapper.ToResult(model);

        Assert.Equal(model.Id, result.Id);
        Assert.Equal("Prior history of COPD", result.Name);
        Assert.Equal(CriterionType.Boolean, result.Type);
        Assert.Null(result.Value);
    }

    [Fact]
    public void ToResult_NumericCriterion_MapsRangeValue()
    {
        var model = new NumericCriterion
        {
            Name = "Respiratory rate",
            Value = new Range
            {
                Min = 30m,
                Max = 120m,
                IsMinExclusive = false,
                IsMaxExclusive = false,
                Unit = "breaths/min",
            },
        };

        var result = _mapper.ToResult(model);

        Assert.Equal(model.Id, result.Id);
        Assert.Equal("Respiratory rate", result.Name);
        Assert.Equal(CriterionType.Numeric, result.Type);
        Assert.NotNull(result.Value);
        Assert.Equal(30m, result.Value.Min);
        Assert.Equal(120m, result.Value.Max);
        Assert.Equal("breaths/min", result.Value.Unit);
    }

    # endregion
}
