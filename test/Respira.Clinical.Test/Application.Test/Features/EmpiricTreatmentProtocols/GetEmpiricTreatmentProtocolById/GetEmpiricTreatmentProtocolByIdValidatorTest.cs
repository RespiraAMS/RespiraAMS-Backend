using Application.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

namespace Application.Test.Features.EmpiricTreatmentProtocols.GetEmpiricTreatmentProtocolById;

public class GetEmpiricTreatmentProtocolByIdValidatorTest
{
    private readonly GetEmpiricTreatmentProtocolByIdValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task GetEmpiricTreatmentProtocolById_ValidId_Success()
    {
        var result = await _validator.ValidateAsync(
            new GetEmpiricTreatmentProtocolByIdQuery { Id = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task GetEmpiricTreatmentProtocolById_EmptyId_Fail()
    {
        // Boundary: Guid.Empty is treated as empty by NotEmpty
        var result = await _validator.ValidateAsync(
            new GetEmpiricTreatmentProtocolByIdQuery { Id = Guid.Empty },
            TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    # endregion
}
