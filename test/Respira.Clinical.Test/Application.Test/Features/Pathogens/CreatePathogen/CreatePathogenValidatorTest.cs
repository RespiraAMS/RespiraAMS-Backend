using Application.Features.Pathogens.CreatePathogen;

namespace Application.Test.Features.Pathogens.CreatePathogen;

public class CreatePathogenValidatorTest
{
    private readonly CreatePathogenValidator _validator = new();

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

    [Theory]
    [InlineData("abc", "", "Description")]
    [InlineData("", "some random", "Name")]
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

}
