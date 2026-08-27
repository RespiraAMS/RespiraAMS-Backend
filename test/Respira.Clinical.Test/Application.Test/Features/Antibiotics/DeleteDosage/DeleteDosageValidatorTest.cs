using Application.Features.Antibiotics.DeleteDosage;

namespace Application.Test.Features.Antibiotics.DeleteDosage;

public class DeleteDosageValidatorTest
{
    private readonly DeleteDosageValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task DeleteDosage_Success()
    {
        var result = await _validator.ValidateAsync(new DeleteDosageCommand
        {
            Id = Guid.CreateVersion7(),
            AntibioticId = Guid.CreateVersion7(),
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, Guid, string> InvalidCommands =
    [
        // Boundary: empty dosage GUID violates NotEmpty on Id
        (Guid.Empty, Guid.CreateVersion7(), "Id"),
        // The antibiotic ID is required by the command contract (same as AddDosage):
        // an empty GUID must be rejected. This case FAILS against the current source
        // because DeleteDosageValidator never defines a rule for AntibioticId
        (Guid.CreateVersion7(), Guid.Empty, "AntibioticId"),
    ];

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task DeleteDosage_Fail(Guid id, Guid antibioticId, string property)
    {
        var result = await _validator.ValidateAsync(new DeleteDosageCommand
        {
            Id = id,
            AntibioticId = antibioticId,
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, x => x.PropertyName == property);
    }

    # endregion
}
