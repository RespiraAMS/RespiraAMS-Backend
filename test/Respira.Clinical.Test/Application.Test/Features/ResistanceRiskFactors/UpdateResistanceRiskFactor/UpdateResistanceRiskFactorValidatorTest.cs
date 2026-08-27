using Application.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Test.Features.ResistanceRiskFactors.UpdateResistanceRiskFactor;

public class UpdateResistanceRiskFactorValidatorTest
{
    private readonly UpdateResistanceRiskFactorValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task UpdateResistanceRiskFactor_BooleanType_Success()
    {
        var result = await _validator.ValidateAsync(new UpdateResistanceRiskFactorCommand
        {
            Id = Guid.CreateVersion7(),
            PathogenId = Guid.CreateVersion7(),
            Name = "Prior antibiotic use in last 90 days",
            Criterion = new UpdateCriterionCommand
            {
                Name = "Prior antibiotic use",
                Type = CriterionType.Boolean,
                Value = null,
            },
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task UpdateResistanceRiskFactor_NumericType_Success()
    {
        var result = await _validator.ValidateAsync(new UpdateResistanceRiskFactorCommand
        {
            Id = Guid.CreateVersion7(),
            PathogenId = Guid.CreateVersion7(),
            Name = "Elevated WBC count",
            Criterion = new UpdateCriterionCommand
            {
                Name = "WBC count",
                Type = CriterionType.Numeric,
                Value = new Range
                {
                    Min = 12,
                    IsMinExclusive = false,
                    Max = decimal.MaxValue,
                    IsMaxExclusive = false,
                    Unit = "x10^9/L",
                },
            },
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, Guid, string, UpdateCriterionCommand, string> InvalidCommands =
    [
        // Boundary: empty GUID violates NotEmpty on Id
        (
            Guid.Empty, Guid.CreateVersion7(), "Prior antibiotic use",
            new UpdateCriterionCommand { Name = "Prior antibiotic use", Type = CriterionType.Boolean, Value = null },
            "Id"
        ),
        // Empty PathogenId violates NotEmpty
        (
            Guid.CreateVersion7(), Guid.Empty, "Prior antibiotic use",
            new UpdateCriterionCommand { Name = "Prior antibiotic use", Type = CriterionType.Boolean, Value = null },
            "PathogenId"
        ),
        // Empty Name violates NotEmpty
        (
            Guid.CreateVersion7(), Guid.CreateVersion7(), "",
            new UpdateCriterionCommand { Name = "Prior antibiotic use", Type = CriterionType.Boolean, Value = null },
            "Name"
        ),
        // Criterion name empty (nested UpdateCriterionValidator)
        (
            Guid.CreateVersion7(), Guid.CreateVersion7(), "Prior antibiotic use",
            new UpdateCriterionCommand { Name = "", Type = CriterionType.Boolean, Value = null },
            "Criterion.Name"
        ),
        // Invalid enum value (out of range) produced by integer cast
        (
            Guid.CreateVersion7(), Guid.CreateVersion7(), "Prior antibiotic use",
            new UpdateCriterionCommand { Name = "Confusion", Type = (CriterionType)999, Value = null },
            "Criterion.Type"
        ),
        // Numeric type but Value missing
        (
            Guid.CreateVersion7(), Guid.CreateVersion7(), "WBC count",
            new UpdateCriterionCommand { Name = "WBC count", Type = CriterionType.Numeric, Value = null },
            "Criterion.Value"
        ),
        // Boolean type must not carry a Value
        (
            Guid.CreateVersion7(), Guid.CreateVersion7(), "Prior antibiotic use",
            new UpdateCriterionCommand
            {
                Name = "Prior antibiotic use", Type = CriterionType.Boolean,
                Value = new Range { Min = 1, IsMinExclusive = false, Max = 2, IsMaxExclusive = false, Unit = "" },
            },
            "Criterion.Value"
        ),
        // Range boundary violated: min greater than max
        (
            Guid.CreateVersion7(), Guid.CreateVersion7(), "WBC count",
            new UpdateCriterionCommand
            {
                Name = "WBC count", Type = CriterionType.Numeric,
                Value = new Range { Min = 20, IsMinExclusive = false, Max = 5, IsMaxExclusive = false, Unit = "x10^9/L" },
            },
            "Criterion.Value"
        ),
    ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(InvalidCommands))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public async Task UpdateResistanceRiskFactor_Fail(Guid id, Guid pathogenId, string name,
        UpdateCriterionCommand criterion, string property)
    {
        var result = await _validator.ValidateAsync(new UpdateResistanceRiskFactorCommand
        {
            Id = id,
            PathogenId = pathogenId,
            Name = name,
            Criterion = criterion,
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, x => x.PropertyName == property);
    }

    # endregion
}
