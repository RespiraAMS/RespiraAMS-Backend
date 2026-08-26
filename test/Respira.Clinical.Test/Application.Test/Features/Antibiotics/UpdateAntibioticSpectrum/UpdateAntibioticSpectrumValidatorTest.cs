using Application.Features.Antibiotics.UpdateAntibioticSpectrum;

namespace Application.Test.Features.Antibiotics.UpdateAntibioticSpectrum;

public class UpdateAntibioticSpectrumValidatorTest
{
    private readonly UpdateAntibioticSpectrumValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task UpdateAntibioticSpectrum_Success()
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticSpectrumCommand
        {
            Id = Guid.CreateVersion7(),
            PathogenIds = [Guid.CreateVersion7(), Guid.CreateVersion7()],
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, List<Guid>, string> InvalidCommands =
    [
        (Guid.Empty, [Guid.CreateVersion7()], "Id"),
        // Boundary: empty list violates the collection-level NotEmpty
        (Guid.CreateVersion7(), [], "PathogenIds"),
        // Boundary: one empty GUID inside the list violates RuleForEach NotEmpty,
        // which prefixes the index for collection items
        (Guid.CreateVersion7(), [Guid.CreateVersion7(), Guid.Empty], "PathogenIds[1]"),
    ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(InvalidCommands))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public async Task UpdateAntibioticSpectrum_Fail(Guid id, List<Guid> pathogenIds, string property)
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticSpectrumCommand
        {
            Id = id,
            PathogenIds = pathogenIds,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateAntibioticSpectrum_AllFieldsInvalid_Fail()
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticSpectrumCommand
        {
            Id = Guid.Empty,
            PathogenIds = [],
        }, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "Id");
        Assert.Contains(result.Errors, x => x.PropertyName == "PathogenIds");
    }

    # endregion
}
