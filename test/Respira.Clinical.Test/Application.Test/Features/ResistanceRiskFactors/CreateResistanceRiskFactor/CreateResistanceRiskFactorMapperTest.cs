using Application.Contracts.Mappers;
using Application.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Range = Domain.Models.Range;

namespace Application.Test.Features.ResistanceRiskFactors.CreateResistanceRiskFactor;

public class CreateResistanceRiskFactorMapperTest
{
    private readonly ICreateMapper<ResistanceRiskFactor, CreateResistanceRiskFactorCommand> _mapper =
        new CreateResistanceRiskFactorMapper(new CreateCriterionMapper());

    # region Happy path

    [Fact]
    public void ToModel_BooleanCriterion_Success()
    {
        var diseaseId = Guid.CreateVersion7();
        var pathogenId = Guid.CreateVersion7();
        var command = new CreateResistanceRiskFactorCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Name = "Prior antibiotic use in last 90 days",
            Criterion = new CreateCriterionCommand
            {
                Name = "Prior antibiotic use",
                Type = CriterionType.Boolean,
                Value = null,
            },
        };

        var model = _mapper.ToModel(command);

        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.Equal(diseaseId, model.DiseaseId);
        Assert.Equal(pathogenId, model.PathogenId);
        Assert.Equal("Prior antibiotic use in last 90 days", model.Name);
        Assert.NotNull(model.Criterion);
        Assert.Equal(model.CriterionId, model.Criterion.Id);
        Assert.IsType<BooleanCriterion>(model.Criterion);
    }

    [Fact]
    public void ToModel_NumericCriterion_Success()
    {
        var diseaseId = Guid.CreateVersion7();
        var pathogenId = Guid.CreateVersion7();
        var value = new Range { Min = 12, IsMinExclusive = false, Max = decimal.MaxValue, IsMaxExclusive = false, Unit = "x10^9/L" };
        var command = new CreateResistanceRiskFactorCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Name = "Elevated white blood cell count",
            Criterion = new CreateCriterionCommand
            {
                Name = "WBC count",
                Type = CriterionType.Numeric,
                Value = value,
            },
        };

        var model = _mapper.ToModel(command);

        Assert.Equal(diseaseId, model.DiseaseId);
        Assert.Equal(pathogenId, model.PathogenId);
        var numeric = Assert.IsType<NumericCriterion>(model.Criterion);
        Assert.Equal(12, numeric.Value.Min);
        Assert.Equal(decimal.MaxValue, numeric.Value.Max);
        Assert.Equal("x10^9/L", numeric.Value.Unit);
        Assert.Equal(model.CriterionId, numeric.Id);
    }

    # endregion
}
