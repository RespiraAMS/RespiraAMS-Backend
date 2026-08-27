using Application.Features.Antibiograms.DeleteAntibiogram;

namespace Application.Test.Features.Antibiogram.DeleteAntibiogram;

public class DeleteAntibiogramValidatorTest
{
    private readonly DeleteAntibiogramValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task DeleteAntibiogram_Success()
    {
        var result = await _validator.ValidateAsync(new DeleteAntibiogramCommand
        {
            Id = Guid.CreateVersion7(),
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    // Boundary: empty GUID is the only invalid value for the single NotEmpty rule
    [Fact]
    public async Task DeleteAntibiogram_EmptyId_Fail()
    {
        var result = await _validator.ValidateAsync(new DeleteAntibiogramCommand
        {
            Id = Guid.Empty,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    # endregion
}
