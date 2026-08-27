using Application.Contracts.Mappers;
using Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Range = Domain.Models.Range;

namespace Application.Test.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;

public class UpdateResistanceRiskFactorMapperTest
{
    private readonly IUpdateMapper<ResistanceRiskFactor, UpdateResistanceRiskFactorCommand> _mapper =
        new UpdateResistanceRiskFactorMapper(new UpdateCriterionMapper());

    # region Happy path

    [Fact]
    public void MapModel_BooleanCriterion_UpdatesNamePathogenAndCriterion_Success()
    {
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);
        var criterion = new BooleanCriterion { Name = "Prior antibiotic use" };
        var model = new ResistanceRiskFactor
        {
            Id = Guid.CreateVersion7(),
            DiseaseId = Guid.CreateVersion7(),
            PathogenId = Guid.CreateVersion7(),
            Name = "Old name",
            CriterionId = criterion.Id,
            Criterion = criterion,
        };

        _mapper.MapModel(model, new UpdateResistanceRiskFactorCommand
        {
            Id = model.Id,
            PathogenId = Guid.CreateVersion7(),
            Name = "Prior antibiotic use in last 90 days",
            Criterion = new UpdateCriterionCommand
            {
                Name = "Prior antibiotic use (updated)",
                Type = CriterionType.Boolean,
                Value = null,
            },
        });

        Assert.Equal("Prior antibiotic use in last 90 days", model.Name);
        Assert.Equal("Prior antibiotic use (updated)", model.Criterion.Name);
        Assert.IsType<BooleanCriterion>(model.Criterion);
        Assert.InRange(model.UpdatedAt, before, DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void MapModel_NumericCriterion_UpdatesValue_Success()
    {
        var initial = new Range { Min = 12, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = "x10^9/L" };
        var criterion = new NumericCriterion { Name = "WBC count", Value = initial };
        var model = new ResistanceRiskFactor
        {
            Id = Guid.CreateVersion7(),
            DiseaseId = Guid.CreateVersion7(),
            PathogenId = Guid.CreateVersion7(),
            Name = "Elevated WBC count",
            CriterionId = criterion.Id,
            Criterion = criterion,
        };
        // CRP >= 100 mg/L (realistic severe-infection marker) as the replacement value
        var updated = new Range { Min = 100, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = "mg/L" };

        _mapper.MapModel(model, new UpdateResistanceRiskFactorCommand
        {
            Id = model.Id,
            PathogenId = Guid.CreateVersion7(),
            Name = "Elevated CRP",
            Criterion = new UpdateCriterionCommand
            {
                Name = "CRP",
                Type = CriterionType.Numeric,
                Value = updated,
            },
        });

        var numeric = Assert.IsType<NumericCriterion>(model.Criterion);
        Assert.Equal("Elevated CRP", model.Name);
        Assert.Equal("CRP", numeric.Name);
        Assert.Equal(100, numeric.Value.Min);
        Assert.Equal(decimal.MaxValue, numeric.Value.Max);
        Assert.Equal("mg/L", numeric.Value.Unit);
    }

    # endregion
}
