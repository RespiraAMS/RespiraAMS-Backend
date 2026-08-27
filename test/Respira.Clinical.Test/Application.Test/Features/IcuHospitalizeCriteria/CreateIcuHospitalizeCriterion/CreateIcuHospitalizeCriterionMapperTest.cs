using Application.Contracts.Mappers;
using Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Range = Domain.Models.Range;

namespace Application.Test.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionMapperTest
{
    private readonly ICreateMapper<IcuHospitalizeCriterion, CreateIcuHospitalizeCriterionCommand> _mapper =
        new CreateIcuHospitalizeCriterionMapper(new CreateCriterionMapper());

    # region Happy path

    [Fact]
    public void ToModel_BooleanCriterion_Success()
    {
        var diseaseId = Guid.CreateVersion7();
        var command = new CreateIcuHospitalizeCriterionCommand
        {
            DiseaseId = diseaseId,
            Criterion = new CreateCriterionCommand
            {
                Name = "Altered mental status",
                Type = CriterionType.Boolean,
                Value = null,
            },
            Score = 1,
        };

        var model = _mapper.ToModel(command);

        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.Equal(diseaseId, model.DiseaseId);
        Assert.Equal(1, model.Score);
        Assert.NotNull(model.Criterion);
        Assert.Equal("Altered mental status", model.Criterion.Name);
        Assert.IsType<BooleanCriterion>(model.Criterion);

        // A boolean criterion carries no range value
        Assert.Equal(model.CriterionId, model.Criterion.Id);
    }

    [Fact]
    public void ToModel_NumericCriterion_Success()
    {
        // Respiratory rate >= 20 breaths/min (CURB-65 component) is a realistic
        // numeric ICU criterion; Max = decimal.MaxValue encodes "no upper bound"
        var diseaseId = Guid.CreateVersion7();
        var value = new Range { Min = 20, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = "breaths/min" };
        var command = new CreateIcuHospitalizeCriterionCommand
        {
            DiseaseId = diseaseId,
            Criterion = new CreateCriterionCommand
            {
                Name = "Respiratory rate",
                Type = CriterionType.Numeric,
                Value = value,
            },
            Score = 3,
        };

        var model = _mapper.ToModel(command);

        Assert.Equal(diseaseId, model.DiseaseId);
        Assert.Equal(3, model.Score);
        var numeric = Assert.IsType<NumericCriterion>(model.Criterion);
        Assert.Equal(20, numeric.Value.Min);
        Assert.Equal(decimal.MaxValue, numeric.Value.Max);
        Assert.Equal("breaths/min", numeric.Value.Unit);
        Assert.Equal(model.CriterionId, numeric.Id);
    }

    # endregion
}
