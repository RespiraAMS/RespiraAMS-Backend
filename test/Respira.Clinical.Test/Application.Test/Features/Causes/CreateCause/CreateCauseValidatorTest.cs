using Application.Features.Causes.CreateCause;
using Domain.Enums;

namespace Application.Test.Features.Causes.CreateCause;

public class CreateCauseValidatorTest
{
    private readonly CreateCauseValidator _validator = new();

    #region Valid command

    // Enum boundaries: first and last defined members of Severity and TreatmentSite
    public static readonly TheoryData<Severity, TreatmentSite> ValidCombos =
        [
            (Severity.Mild, TreatmentSite.Outpatient), // Mild + Outpatient: both lower boundaries
            (Severity.Moderate, TreatmentSite.Inpatient), // Moderate + Inpatient: both middle boundaries
            (Severity.Severe, TreatmentSite.IntensiveCareUnit), // Severe + ICU: both upper boundaries
            (Severity.Severe, TreatmentSite.Outpatient), // Severe + Outpatient: both upper boundaries
        ];

    [Theory]
    [MemberData(nameof(ValidCombos))]
    public async Task CreateCause_Success(Severity severity, TreatmentSite treatmentSite)
    {
        var result = await _validator.ValidateAsync(new CreateCauseCommand
        {
            DiseaseId = Guid.CreateVersion7(),
            PathogenId = Guid.CreateVersion7(),
            Severity = severity,
            TreatmentSite = treatmentSite,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, Guid, Severity, TreatmentSite, string> InvalidCommands =
    [
        // Boundary: empty GUID violates NotEmpty on DiseaseId
        (Guid.Empty, Guid.CreateVersion7(), Severity.Mild, TreatmentSite.Outpatient, "DiseaseId"),
        // Boundary: empty GUID violates NotEmpty on PathogenId
        (Guid.CreateVersion7(), Guid.Empty, Severity.Mild, TreatmentSite.Outpatient, "PathogenId"),
        // Boundary: 999 is outside every defined Severity member
        (Guid.CreateVersion7(), Guid.CreateVersion7(), (Severity)999, TreatmentSite.Outpatient, "Severity"),
        // Boundary: 999 is outside every defined TreatmentSite member
        (Guid.CreateVersion7(), Guid.CreateVersion7(), Severity.Mild, (TreatmentSite)999, "TreatmentSite"),
    ];

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task CreateCause_Fail(Guid diseaseId, Guid pathogenId, Severity severity, TreatmentSite treatmentSite, string property)
    {
        var result = await _validator.ValidateAsync(new CreateCauseCommand
        {
            DiseaseId = diseaseId,
            PathogenId = pathogenId,
            Severity = severity,
            TreatmentSite = treatmentSite,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    # endregion
}
