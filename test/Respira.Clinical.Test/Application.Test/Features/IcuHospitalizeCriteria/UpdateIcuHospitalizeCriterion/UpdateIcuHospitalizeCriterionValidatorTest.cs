using Application.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Test.Features.IcuHospitalizeCriteria.UpdateIcuHospitalizeCriterion;

public class UpdateIcuHospitalizeCriterionValidatorTest
{
    private readonly UpdateIcuHospitalizeCriterionValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task UpdateIcuHospitalizeCriterion_BooleanType_Success()
    {
        var result = await _validator.ValidateAsync(new UpdateIcuHospitalizeCriterionCommand
        {
            Id = Guid.CreateVersion7(),
            Criterion = new UpdateCriterionCommand
            {
                Name = "Altered mental status",
                Type = CriterionType.Boolean,
                Value = null,
            },
            // Boundary of GreaterThan(0): smallest valid score
            Score = 1,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task UpdateIcuHospitalizeCriterion_NumericType_Success()
    {
        // Respiratory rate >= 20 breaths/min with no upper bound
        var result = await _validator.ValidateAsync(new UpdateIcuHospitalizeCriterionCommand
        {
            Id = Guid.CreateVersion7(),
            Criterion = new UpdateCriterionCommand
            {
                Name = "Respiratory rate",
                Type = CriterionType.Numeric,
                Value = new Range
                {
                    Min = 20,
                    IsMinExclusive = false,
                    Max = decimal.MaxValue,
                    IsMaxExclusive = false,
                    Unit = "breaths/min",
                },
            },
            // Boundary of GreaterThan(0): mid realistic NEWS2 score
            Score = 3,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    #endregion

    #region Invalid command

    public static readonly TheoryData<Guid, UpdateCriterionCommand, int, string> InvalidCommands =
    [
       // Boundary: empty GUID violates NotEmpty on Id
       (
            Guid.Empty,
            new UpdateCriterionCommand { Name = "Altered mental status", Type = CriterionType.Boolean, Value = null },
            1, "Id"
        ),
        // Criterion name empty (nested UpdateCriterionValidator)
        (
            Guid.CreateVersion7(),
            new UpdateCriterionCommand { Name = "", Type = CriterionType.Boolean, Value = null },
            1, "Criterion.Name"
        ),
        // Numeric type but Value missing
        (
            Guid.CreateVersion7(),
            new UpdateCriterionCommand { Name = "Respiratory rate", Type = CriterionType.Numeric, Value = null },
            1, "Criterion.Value"
        ),
        // Boolean type must not carry a Value
        (
            Guid.CreateVersion7(),
            new UpdateCriterionCommand
            {
                Name = "Altered mental status", Type = CriterionType.Boolean,
                Value = new Range { Min = 1, IsMinExclusive = false, Max = 2, IsMaxExclusive = false, Unit = "" },
            },
            1, "Criterion.Value"
        ),
        // Range boundary violated: min greater than max
        (
            Guid.CreateVersion7(),
            new UpdateCriterionCommand
            {
                Name = "Systolic BP", Type = CriterionType.Numeric,
                Value = new Range { Min = 90, IsMinExclusive = false, Max = 40, IsMaxExclusive = false, Unit = "mmHg" },
            },
            1, "Criterion.Value"
        ),
        // Score boundary violated: 0 is not GreaterThan(0)
        (
            Guid.CreateVersion7(),
            new UpdateCriterionCommand { Name = "Confusion", Type = CriterionType.Boolean, Value = null },
            0, "Score"
        ),
        // Negative score is also not GreaterThan(0)
        (
            Guid.CreateVersion7(),
            new UpdateCriterionCommand { Name = "Confusion", Type = CriterionType.Boolean, Value = null },
            -2, "Score"
        ),
        // Invalid enum value (out of range) produced by integer cast -- SOURCE GAP
        (
            Guid.CreateVersion7(),
            new UpdateCriterionCommand { Name = "Confusion", Type = (CriterionType)999, Value = null },
            1, "Criterion.Type"
        ),
    ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(InvalidCommands))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public async Task UpdateIcuHospitalizeCriterion_Fail(Guid id, UpdateCriterionCommand criterion,
        int score, string property)
    {
        var result = await _validator.ValidateAsync(new UpdateIcuHospitalizeCriterionCommand
        {
            Id = id,
            Criterion = criterion,
            Score = score,
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, x => x.PropertyName == property);
    }

    # endregion
}
