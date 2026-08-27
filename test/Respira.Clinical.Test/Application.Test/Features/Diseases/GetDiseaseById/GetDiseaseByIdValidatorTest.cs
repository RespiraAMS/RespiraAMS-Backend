using Application.Features.Diseases.GetDiseaseById;

namespace Application.Test.Features.Diseases.GetDiseaseById;

public class GetDiseaseByIdValidatorTest
{
    private readonly GetDiseaseByIdValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task GetDiseaseById_ValidCommand_Success()
    {
        var result = await _validator.ValidateAsync(
            new GetDiseaseByIdQuery { Id = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task GetDiseaseById_EmptyId_Fail()
    {
        var command = new GetDiseaseByIdQuery { Id = Guid.Empty };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    # endregion
}
