using Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause;
using Respira.Clinical.Domain.Enums;

namespace Respira.Application.Test.Features.SuspectedCauses.CreateSuspectedCause
{
    public class CreateSuspectedCauseValidatorTest
    {
        private readonly CreateSuspectedCauseValidator _validator = new();

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
            var result = await _validator.ValidateAsync(new CreateSuspectedCauseCommand
            {
                PathogenId = Guid.CreateVersion7(),
                Severity = severity,
                TreatmentSite = treatmentSite,
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        public static readonly TheoryData<Guid, Severity, TreatmentSite, string> InvalidCommands =
        [
            // Boundary: empty GUID violates NotEmpty on PathogenId
            (Guid.Empty, Severity.Mild, TreatmentSite.Outpatient, "PathogenId"),
            // Boundary: 999 is outside every defined Severity member
            (Guid.CreateVersion7(), (Severity)999, TreatmentSite.Outpatient, "Severity"),
            // Boundary: 999 is outside every defined TreatmentSite member
            (Guid.CreateVersion7(), Severity.Mild, (TreatmentSite)999, "TreatmentSite"),
        ];

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public async Task CreateCause_Fail(Guid pathogenId, Severity severity, TreatmentSite treatmentSite, string property)
        {
            var result = await _validator.ValidateAsync(new CreateSuspectedCauseCommand
            {
                PathogenId = pathogenId,
                Severity = severity,
                TreatmentSite = treatmentSite,
            }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(property, result.Errors[0].PropertyName);
        }

        #endregion
    }
}
