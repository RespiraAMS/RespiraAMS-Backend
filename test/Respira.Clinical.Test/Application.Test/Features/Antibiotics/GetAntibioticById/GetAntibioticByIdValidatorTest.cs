using Application.Features.Antibiotics.GetAntibioticById;

namespace Application.Test.Features.Antibiotics.GetAntibioticById;

public class GetAntibioticByIdValidatorTest
{
    private readonly GetAntibioticByIdValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task GetAntibioticById_Success()
    {
        var result = await _validator.ValidateAsync(
            new GetAntibioticByIdQuery { Id = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task GetAntibioticById_EmptyId_Fail()
    {
        // Boundary: Guid.Empty is treated as empty by NotEmpty
        var result = await _validator.ValidateAsync(
            new GetAntibioticByIdQuery { Id = Guid.Empty },
            TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    # endregion
}
