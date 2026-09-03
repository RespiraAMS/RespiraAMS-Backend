using Application.Features.Causes.DeleteCause;

namespace Application.Test.Features.Causes.DeleteCause;

public class DeleteCauseValidatorTest
{
    private readonly DeleteCauseValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task DeleteCause_Success()
    {
        var result = await _validator.ValidateAsync(new DeleteCauseCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);
        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    // Boundary: empty GUID is the only invalid value for the single NotEmpty rule
    [Fact]
    public async Task DeleteCause_EmptyId_Fail()
    {
        var result = await _validator.ValidateAsync(new DeleteCauseCommand(Guid.Empty), TestContext.Current.CancellationToken);
        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    # endregion
}
