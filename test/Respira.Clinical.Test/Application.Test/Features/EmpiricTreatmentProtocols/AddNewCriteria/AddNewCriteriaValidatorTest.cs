using Application.Features.EmpiricTreatmentProtocols.AddNewCriteria;
using Application.Features.Shared.ManageCriterion;
using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Test.Features.EmpiricTreatmentProtocols.AddNewCriteria;

public class AddNewCriteriaValidatorTest
{
    private readonly AddNewCriteriaValidator _validator = new();

    private static CreateCriterionCommand ValidBooleanCriterion(string name = "Prior history of COPD") => new()
    {
        Name = name,
        Type = CriterionType.Boolean,
    };

    private static CreateCriterionCommand ValidNumericCriterion(string name = "Respiratory rate") => new()
    {
        Name = name,
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

    # region Valid command

    [Fact]
    public async Task AddNewCriteria_ValidCommand_Success()
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.CreateVersion7(),
            Criteria =
            [
                ValidBooleanCriterion(),
                ValidNumericCriterion(),
            ],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task AddNewCriteria_EmptyId_Fail()
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.Empty,
            Criteria = [ValidBooleanCriterion()],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task AddNewCriteria_EmptyCriteriaList_Fail()
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.CreateVersion7(),
            Criteria = [],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Criteria", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("   ", "Name")]
    public async Task AddNewCriteria_CriterionEmptyName_Fail(string name, string property)
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.CreateVersion7(),
            Criteria = [ValidBooleanCriterion(name)],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("Criteria") && e.PropertyName.EndsWith(property));
    }

    [Fact]
    public async Task AddNewCriteria_CriterionInvalidType_Fail()
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.CreateVersion7(),
            Criteria =
            [
                new() { Name = "Respiratory rate", Type = (CriterionType)99 },
            ],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("Criteria") && e.PropertyName.EndsWith("Type"));
    }

    // Boundary value technique: numeric criterion requires a value, so a missing range is rejected
    [Fact]
    public async Task AddNewCriteria_NumericWithoutValue_Fail()
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.CreateVersion7(),
            Criteria =
            [
                new() { Name = "Respiratory rate", Type = CriterionType.Numeric, Value = null },
            ],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("Criteria") && e.PropertyName.Contains("Value"));
    }

    [Fact]
    public async Task AddNewCriteria_BooleanWithValue_Fail()
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.CreateVersion7(),
            Criteria =
            [
                new()
                {
                    Name = "Prior history of COPD",
                    Type = CriterionType.Boolean,
                    Value = new Range { Min = 1m, Max = 2m, IsMinExclusive = false, IsMaxExclusive = false, Unit = "x" },
                },
            ],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("Criteria") && e.PropertyName.Contains("Value"));
    }

    // Boundary value technique: range requires Min <= Max
    [Fact]
    public async Task AddNewCriteria_NumericRangeMinGreaterThanMax_Fail()
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.CreateVersion7(),
            Criteria =
            [
                new()
                {
                    Name = "Respiratory rate",
                    Type = CriterionType.Numeric,
                    Value = new Range { Min = 120m, Max = 30m, IsMinExclusive = false, IsMaxExclusive = false, Unit = "breaths/min" },
                },
            ],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("Criteria") && e.PropertyName.Contains("Value"));
    }

    [Fact]
    public async Task AddNewCriteria_NumericRangeEmptyStringUnit_Fail()
    {
        var command = new AddNewCriteriaCommand
        {
            Id = Guid.CreateVersion7(),
            Criteria =
            [
                new()
                {
                    Name = "Respiratory rate",
                    Type = CriterionType.Numeric,
                    Value = new Range { Min = 30m, Max = 120m, IsMinExclusive = false, IsMaxExclusive = false, Unit = "   " },
                },
            ],
        };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("Criteria") && e.PropertyName.Contains("Value"));
    }

    # endregion
}
