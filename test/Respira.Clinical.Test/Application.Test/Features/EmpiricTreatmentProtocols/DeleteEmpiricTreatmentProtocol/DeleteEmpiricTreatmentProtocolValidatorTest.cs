using Application.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

namespace Application.Test.Features.EmpiricTreatmentProtocols.DeleteEmpiricTreatmentProtocol;

public class DeleteEmpiricTreatmentProtocolValidatorTest
{
    private readonly DeleteEmpiricTreatmentProtocolValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task DeleteEmpiricTreatmentProtocol_ValidId_Success()
    {
        var result = await _validator.ValidateAsync(new DeleteEmpiricTreatmentProtocolCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);
        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task DeleteEmpiricTreatmentProtocol_EmptyId_Fail()
    {
        // Boundary: Guid.Empty is treated as empty by NotEmpty
        var result = await _validator.ValidateAsync(new DeleteEmpiricTreatmentProtocolCommand(Guid.Empty), TestContext.Current.CancellationToken);
        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    # endregion
}
