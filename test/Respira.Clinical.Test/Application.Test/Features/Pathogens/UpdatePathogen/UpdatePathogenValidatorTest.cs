using Application.Features.Pathogens.UpdatePathogen;

namespace Application.Test.Features.Pathogens.UpdatePathogen;

public class UpdatePathogenValidatorTest
{
    private readonly UpdatePathogenValidator _validator = new();

    private static readonly Guid ExistingPathogenId = Guid.CreateVersion7();

    public static readonly TheoryData<Guid, string, string> ValidCommands =
    [
        (ExistingPathogenId, "Streptococcus pneumoniae", "Gram-positive diplococcus"),
        (ExistingPathogenId, "Haemophilus influenzae", "Gram-negative coccobacillus"),
    ];

    # region Valid command

    [Theory]
    [MemberData(nameof(ValidCommands))]
    public async Task UpdatePathogen_Success(Guid id, string name, string description)
    {
        var result = await _validator.ValidateAsync(new UpdatePathogenCommand
        {
            Id = id,
            Name = name,
            Description = description
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Theory]
    [InlineData("0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D", "", "some description", "Name")]
    [InlineData("0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D", "   ", "some description", "Name")]
    [InlineData("0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D", "some name", "", "Description")]
    [InlineData("0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D", "some name", " ", "Description")]
    // Guid.Empty is treated as empty by NotEmpty
    [InlineData("00000000-0000-0000-0000-000000000000", "some name", "some description", "Id")]
    public async Task UpdatePathogen_Fail(string id, string name, string description, string property)
    {
        var result = await _validator.ValidateAsync(new UpdatePathogenCommand
        {
            Id = Guid.Parse(id),
            Name = name,
            Description = description
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdatePathogen_AllFieldsEmpty_Fail()
    {
        var result = await _validator.ValidateAsync(new UpdatePathogenCommand
        {
            Id = Guid.Empty,
            Name = "",
            Description = ""
        }, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "Id");
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "Description");
    }

    # endregion
}
