using Application.Features.Diseases.UpdateDisease;

namespace Application.Test.Features.Diseases.UpdateDisease;

public class UpdateDiseaseValidatorTest
{
    private readonly UpdateDiseaseValidator _validator = new();

    private static UpdateDiseaseCommand ValidCommand(int threshold = 3) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "Community-Acquired Pneumonia",
        Description = "Infection of the lung parenchyma acquired outside of a healthcare setting",
        IcuScoreThreshold = threshold,
    };

    # region Valid command

    [Fact]
    public async Task UpdateDisease_ValidCommand_Success()
    {
        var result = await _validator.ValidateAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary value technique: IcuScoreThreshold of 1 is the smallest accepted value
    [Fact]
    public async Task UpdateDisease_ThresholdBoundaryMin_Success()
    {
        var result = await _validator.ValidateAsync(ValidCommand(threshold: 1), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task UpdateDisease_EmptyId_Fail()
    {
        var command = ValidCommand();
        command.Id = Guid.Empty;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("   ", "Name")]
    public async Task UpdateDisease_InvalidName_Fail(string name, string property)
    {
        var command = ValidCommand();
        command.Name = name;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData("   ", "Description")]
    public async Task UpdateDisease_InvalidDescription_Fail(string description, string property)
    {
        var command = ValidCommand();
        command.Description = description;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    // Boundary value technique: 0 is just below the valid range (must be > 0)
    [Fact]
    public async Task UpdateDisease_ThresholdZero_Fail()
    {
        var command = ValidCommand();
        command.IcuScoreThreshold = 0;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("IcuScoreThreshold", result.Errors[0].PropertyName);
    }

    # endregion
}
