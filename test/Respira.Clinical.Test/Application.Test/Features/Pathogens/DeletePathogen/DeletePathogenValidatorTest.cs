using Application.Features.Pathogens.DeletePathogen;

namespace Application.Test.Features.Pathogens.DeletePathogen;

public class DeletePathogenValidatorTest
{
    private readonly DeletePathogenValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task DeletePathogen_Success()
    {
        var result = await _validator.ValidateAsync(
            new DeletePathogenCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task DeletePathogen_EmptyId_Fail()
    {
        // Boundary: Guid.Empty is treated as empty by NotEmpty
        var result = await _validator.ValidateAsync(
            new DeletePathogenCommand(Guid.Empty), TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    # endregion
}
