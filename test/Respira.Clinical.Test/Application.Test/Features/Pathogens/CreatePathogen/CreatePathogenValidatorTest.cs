using Application.Features.Pathogens.CreatePathogen;

namespace Application.Test.Features.Pathogens.CreatePathogen;

public class CreatePathogenValidatorTest
{
    private readonly CreatePathogenValidator _validator = new();

    # region Valid command

    [Theory]
    [InlineData("abc", "abc description")]
    [InlineData("xyz", "some random")]
    public async Task CreatePathogen_Success(string name, string description)
    {
        var result = await _validator.ValidateAsync(new CreatePathogenCommand
        {
            Name = name,
            Description = description
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Theory]
    [InlineData("abc", "", "Description")]
    [InlineData("", "some random", "Name")]
    // Whitespace-only value is treated as empty by NotEmpty
    [InlineData("abc", "   ", "Description")]
    [InlineData(" ", "some random", "Name")]
    public async Task CreatePathogen_Fail(string name, string description, string property)
    {
        var result = await _validator.ValidateAsync(new CreatePathogenCommand
        {
            Name = name,
            Description = description
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreatePathogen_AllFieldsEmpty_Fail()
    {
        var result = await _validator.ValidateAsync(new CreatePathogenCommand
        {
            Name = "",
            Description = ""
        }, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "Description");
    }

    # endregion
}
