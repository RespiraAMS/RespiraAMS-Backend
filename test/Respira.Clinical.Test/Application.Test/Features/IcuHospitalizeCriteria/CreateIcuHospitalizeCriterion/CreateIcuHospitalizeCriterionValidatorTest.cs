using Application.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Test.Features.IcuHospitalizeCriteria.CreateIcuHospitalizeCriterion;

public class CreateIcuHospitalizeCriterionValidatorTest
{
    private readonly CreateIcuHospitalizeCriterionValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task CreateIcuHospitalizeCriterion_BooleanType_Success()
    {
        var result = await _validator.ValidateAsync(new CreateIcuHospitalizeCriterionCommand
        {
            DiseaseId = Guid.CreateVersion7(),
            Criterion = new CreateCriterionCommand
            {
                Name = "Altered mental status",
                Type = CriterionType.Boolean,
                Value = null,
            },
            // Boundary of GreaterThan(0): the smallest valid score
            Score = 1,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CreateIcuHospitalizeCriterion_NumericType_Success()
    {
        // Respiratory rate >= 20 breaths/min with no upper bound
        var result = await _validator.ValidateAsync(new CreateIcuHospitalizeCriterionCommand
        {
            DiseaseId = Guid.CreateVersion7(),
            Criterion = new CreateCriterionCommand
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
            // Boundary of GreaterThan(0): a mid realistic NEWS2 score
            Score = 3,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, CreateCriterionCommand, int, string> InvalidCommands =
    [
        // Boundary: empty GUID violates NotEmpty on DiseaseId
        (
            Guid.Empty,
            new CreateCriterionCommand { Name = "Altered mental status", Type = CriterionType.Boolean, Value = null },
            1, "DiseaseId"
        ),
        // Criterion name empty (nested CreateCriterionValidator)
        (
            Guid.CreateVersion7(),
            new CreateCriterionCommand { Name = "", Type = CriterionType.Boolean, Value = null },
            1, "Criterion.Name"
        ),
        // Invalid enum value (out of range) produced by integer cast
        (
            Guid.CreateVersion7(),
            new CreateCriterionCommand { Name = "Confusion", Type = (CriterionType)999, Value = null },
            1, "Criterion.Type"
        ),
        // Numeric type but Value missing
        (
            Guid.CreateVersion7(),
            new CreateCriterionCommand { Name = "Respiratory rate", Type = CriterionType.Numeric, Value = null },
            1, "Criterion.Value"
        ),
        // Boolean type must not carry a Value
        (
            Guid.CreateVersion7(),
            new CreateCriterionCommand
            {
                Name = "Altered mental status", Type = CriterionType.Boolean,
                Value = new Range { Min = 1, IsMinExclusive = false, Max = 2, IsMaxExclusive = false, Unit = "" },
            },
            1, "Criterion.Value"
        ),
        // Range boundary violated: min greater than max
        (
            Guid.CreateVersion7(),
            new CreateCriterionCommand
            {
                Name = "Systolic BP", Type = CriterionType.Numeric,
                Value = new Range { Min = 90, IsMinExclusive = false, Max = 40, IsMaxExclusive = false, Unit = "mmHg" },
            },
            1, "Criterion.Value"
        ),
        // Score boundary violated: 0 is not GreaterThan(0)
        (
            Guid.CreateVersion7(),
            new CreateCriterionCommand { Name = "Confusion", Type = CriterionType.Boolean, Value = null },
            0, "Score"
        ),
        // Negative score is also not GreaterThan(0)
        (
            Guid.CreateVersion7(),
            new CreateCriterionCommand { Name = "Confusion", Type = CriterionType.Boolean, Value = null },
            -2, "Score"
        ),
    ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(InvalidCommands))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public async Task CreateIcuHospitalizeCriterion_Fail(Guid diseaseId, CreateCriterionCommand criterion,
        int score, string property)
    {
        var result = await _validator.ValidateAsync(new CreateIcuHospitalizeCriterionCommand
        {
            DiseaseId = diseaseId,
            Criterion = criterion,
            Score = score,
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, x => x.PropertyName == property);
    }

    # endregion
}
