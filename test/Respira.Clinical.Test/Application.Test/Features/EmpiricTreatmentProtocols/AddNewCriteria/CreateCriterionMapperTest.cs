using Application.Contracts.Mappers;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Domain.Models;
using Respira.ServiceDefaults.Exceptions;
using Range = Domain.Models.Range;

namespace Application.Test.Features.EmpiricTreatmentProtocols.AddNewCriteria;

public class CreateCriterionMapperTest
{
    private readonly ICreateMapper<Criterion, CreateCriterionCommand> _mapper = new CreateCriterionMapper();

    # region Happy path

    [Fact]
    public void ToModel_BooleanCriterion_MapsName()
    {
        var command = new CreateCriterionCommand
        {
            Name = "Prior history of COPD",
            Type = CriterionType.Boolean,
        };

        var model = _mapper.ToModel(command);

        var boolean = Assert.IsType<BooleanCriterion>(model);
        Assert.Equal("Prior history of COPD", boolean.Name);
        Assert.NotEqual(Guid.Empty, boolean.Id);
    }

    [Fact]
    public void ToModel_NumericCriterion_MapsNameAndValue()
    {
        var command = new CreateCriterionCommand
        {
            Name = "Respiratory rate",
            Type = CriterionType.Numeric,
            Value = new Range
            {
                Min = 30m,
                Max = 120m,
                IsMinExclusive = false,
                IsMaxExclusive = false,
                Unit = "breaths/min",
            },
        };

        var model = _mapper.ToModel(command);

        var numeric = Assert.IsType<NumericCriterion>(model);
        Assert.Equal("Respiratory rate", numeric.Name);
        Assert.NotNull(numeric.Value);
        Assert.Equal(30m, numeric.Value.Min);
        Assert.Equal(120m, numeric.Value.Max);
        Assert.Equal("breaths/min", numeric.Value.Unit);
    }

    # endregion

    # region Defensive path

    [Fact]
    public void ToModel_NumericWithoutValue_Throws()
    {
        // The mapper defensively rejects a numeric criterion whose range is missing.
        // In the normal pipeline this is already blocked by CreateCriterionValidator,
        // but the mapper still guards against it.
        var command = new CreateCriterionCommand
        {
            Name = "Respiratory rate",
            Type = CriterionType.Numeric,
            Value = null,
        };

        Assert.Throws<UnexpectedException>(() => _mapper.ToModel(command));
    }

    # endregion
}
