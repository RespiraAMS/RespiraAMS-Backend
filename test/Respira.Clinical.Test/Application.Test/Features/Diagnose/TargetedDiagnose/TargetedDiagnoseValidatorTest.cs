using Application.Features.Diagnose.TargetedDiagnose;

namespace Application.Test.Features.Diagnose.TargetedDiagnose;

public class TargetedDiagnoseValidatorTest
{
    private readonly TargetedDiagnoseValidator _validator = new();

    private static TargetedDiagnoseQuery ValidCommand() => new()
    {
        PathogenId = Guid.CreateVersion7(),
        DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-50),
        IsMale = true,
        Weight = 70m,
        Height = 1.7m,
        SerumCreatine = 1.0m,
    };

    # region Valid command

    [Fact]
    public async Task TargetedDiagnose_ValidCommand_Success()
    {
        var result = await _validator.ValidateAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task TargetedDiagnose_EmptyPathogenId_Fail()
    {
        var command = ValidCommand();
        command.PathogenId = Guid.Empty;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("PathogenId", result.Errors[0].PropertyName);
    }

    // Boundary value technique: Weight = 0 is just below the valid range (must be > 0)
    [Fact]
    public async Task TargetedDiagnose_WeightZero_Fail()
    {
        var command = ValidCommand();
        command.Weight = 0;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Weight", result.Errors[0].PropertyName);
    }

    // Boundary value technique: Height = 0 is just below the valid range (must be > 0)
    [Fact]
    public async Task TargetedDiagnose_HeightZero_Fail()
    {
        var command = ValidCommand();
        command.Height = 0;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Height", result.Errors[0].PropertyName);
    }

    // Boundary value technique: SerumCreatine = 0 is just below the valid range (must be > 0)
    [Fact]
    public async Task TargetedDiagnose_SerumCreatineZero_Fail()
    {
        var command = ValidCommand();
        command.SerumCreatine = 0;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("SerumCreatine", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task TargetedDiagnose_FutureDateOfBirth_Fail()
    {
        var command = ValidCommand();
        command.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("DateOfBirth", result.Errors[0].PropertyName);
    }

    # endregion
}
