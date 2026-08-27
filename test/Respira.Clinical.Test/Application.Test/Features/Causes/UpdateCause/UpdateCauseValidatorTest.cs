using Application.Features.Causes.UpdateCause;
using Domain.Enums;

namespace Application.Test.Features.Causes.UpdateCause;

public class UpdateCauseValidatorTest
{
    private readonly UpdateCauseValidator _validator = new();

    # region Valid command

    // Enum boundaries: first and last defined members of Severity and TreatmentSite
    public static readonly TheoryData<Severity, TreatmentSite> ValidCombos =
    [
        (Severity.Mild, TreatmentSite.Outpatient),          // both lower boundaries
        (Severity.Mild, TreatmentSite.IntensiveCareUnit),
        (Severity.Severe, TreatmentSite.Outpatient),
        (Severity.Severe, TreatmentSite.IntensiveCareUnit), // both upper boundaries
        (Severity.Moderate, TreatmentSite.Inpatient),       // middle values
    ];

    [Theory]
    [MemberData(nameof(ValidCombos))]
    public async Task UpdateCause_Success(Severity severity, TreatmentSite treatmentSite)
    {
        var result = await _validator.ValidateAsync(new UpdateCauseCommand
        {
            Id = Guid.CreateVersion7(),
            Severity = severity,
            TreatmentSite = treatmentSite,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, Severity, TreatmentSite, string> InvalidCommands =
    [
        // Boundary: empty GUID violates NotEmpty on Id
        (Guid.Empty, Severity.Mild, TreatmentSite.Outpatient, "Id"),
        // Invalid enum values are produced by casting an out-of-range integer
        (Guid.CreateVersion7(), (Severity)999, TreatmentSite.Outpatient, "Severity"),
        (Guid.CreateVersion7(), Severity.Mild, (TreatmentSite)999, "TreatmentSite"),
    ];

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task UpdateCause_Fail(Guid id, Severity severity, TreatmentSite treatmentSite,
        string property)
    {
        var result = await _validator.ValidateAsync(new UpdateCauseCommand
        {
            Id = id,
            Severity = severity,
            TreatmentSite = treatmentSite,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    # endregion
}
