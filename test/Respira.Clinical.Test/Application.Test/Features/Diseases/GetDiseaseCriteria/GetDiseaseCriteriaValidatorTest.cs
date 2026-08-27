using Application.Features.Diseases.GetDiseaseCriteria;

namespace Application.Test.Features.Diseases.GetDiseaseCriteria;

public class GetDiseaseCriteriaValidatorTest
{
    private readonly GetDiseaseCriteriaValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task GetDiseaseCriteria_ValidCommand_Success()
    {
        var result = await _validator.ValidateAsync(
            new GetDiseaseCriteriaQuery { Id = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task GetDiseaseCriteria_EmptyId_Fail()
    {
        var command = new GetDiseaseCriteriaQuery { Id = Guid.Empty };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    # endregion
}
