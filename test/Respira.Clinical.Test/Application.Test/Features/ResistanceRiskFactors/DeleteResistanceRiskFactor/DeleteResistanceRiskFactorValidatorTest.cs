using Application.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

namespace Application.Test.Features.ResistanceRiskFactors.DeleteResistanceRiskFactor;

public class DeleteResistanceRiskFactorValidatorTest
{
    private readonly DeleteResistanceRiskFactorValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task DeleteResistanceRiskFactor_ValidId_Success()
    {
        var result = await _validator.ValidateAsync(new DeleteResistanceRiskFactorCommand
        {
            Id = Guid.CreateVersion7(),
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid> InvalidIds =
    [
        // Boundary: empty GUID violates NotEmpty on Id
        Guid.Empty,
    ];

    [Theory]
    [MemberData(nameof(InvalidIds))]
    public async Task DeleteResistanceRiskFactor_Fail(Guid id)
    {
        var result = await _validator.ValidateAsync(new DeleteResistanceRiskFactorCommand
        {
            Id = id,
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, x => x.PropertyName == "Id");
    }

    # endregion
}
