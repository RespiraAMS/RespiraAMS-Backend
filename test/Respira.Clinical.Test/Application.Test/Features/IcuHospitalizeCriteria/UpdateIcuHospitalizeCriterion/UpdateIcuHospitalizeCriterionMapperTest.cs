using Application.Contracts.Mappers;
using Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Range = Domain.Models.Range;

namespace Application.Test.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public class UpdateIcuHospitalizeCriterionMapperTest
{
    private readonly IUpdateMapper<IcuHospitalizeCriterion, UpdateIcuHospitalizeCriterionCommand> _mapper =
        new UpdateIcuHospitalizeCriterionMapper(new UpdateCriterionMapper());

    # region Happy path

    [Fact]
    public void MapModel_BooleanCriterion_UpdatesNameAndScore_Success()
    {
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);
        var criterion = new BooleanCriterion { Name = "Altered mental status" };
        var model = new IcuHospitalizeCriterion
        {
            Id = Guid.CreateVersion7(),
            DiseaseId = Guid.CreateVersion7(),
            CriterionId = criterion.Id,
            Criterion = criterion,
            Score = 1,
        };

        _mapper.MapModel(model, new UpdateIcuHospitalizeCriterionCommand
        {
            Id = model.Id,
            Criterion = new UpdateCriterionCommand
            {
                Name = "Confusion",
                Type = CriterionType.Boolean,
                Value = null,
            },
            // Boundary of GreaterThan(0): smallest valid score
            Score = 1,
        });

        Assert.Equal("Confusion", model.Criterion.Name);
        Assert.Equal(1, model.Score);
        Assert.InRange(model.UpdatedAt, before, DateTimeOffset.UtcNow.AddSeconds(5));
        // Type must remain Boolean (it is immutable for a boolean criterion)
        Assert.IsType<BooleanCriterion>(model.Criterion);
    }

    [Fact]
    public void MapModel_NumericCriterion_UpdatesValueAndScore_Success()
    {
        var initial = new Range { Min = 20, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = "breaths/min" };
        var criterion = new NumericCriterion { Name = "Respiratory rate", Value = initial };
        var model = new IcuHospitalizeCriterion
        {
            Id = Guid.CreateVersion7(),
            DiseaseId = Guid.CreateVersion7(),
            CriterionId = criterion.Id,
            Criterion = criterion,
            Score = 3,
        };
        // Systolic BP < 90 mmHg (NEWS2 lowest band) as the replacement value
        var updated = new Range { Min = decimal.MinValue, IsMinExclusive = false, Max = 90, IsMaxExclusive = false, Unit = "mmHg" };

        _mapper.MapModel(model, new UpdateIcuHospitalizeCriterionCommand
        {
            Id = model.Id,
            Criterion = new UpdateCriterionCommand
            {
                Name = "Systolic blood pressure",
                Type = CriterionType.Numeric,
                Value = updated,
            },
            Score = 3,
        });

        var numeric = Assert.IsType<NumericCriterion>(model.Criterion);
        Assert.Equal("Systolic blood pressure", numeric.Name);
        Assert.Equal(decimal.MinValue, numeric.Value.Min);
        Assert.Equal(90, numeric.Value.Max);
        Assert.Equal("mmHg", numeric.Value.Unit);
        Assert.Equal(3, model.Score);
    }

    # endregion
}
