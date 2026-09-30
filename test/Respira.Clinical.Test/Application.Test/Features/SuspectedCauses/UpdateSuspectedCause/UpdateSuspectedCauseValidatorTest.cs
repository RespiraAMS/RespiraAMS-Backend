using Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause;
using Respira.Clinical.Domain.Enums;

namespace Respira.Application.Test.Features.SuspectedCauses.UpdateSuspectedCause
{
    public class UpdateSuspectedCauseValidatorTest
    {
        private readonly UpdateSuspectedCauseValidator _validator = new();

        #region Valid command

        // Enum boundaries: first and last defined members of Severity and TreatmentSite
        public static readonly TheoryData<Guid, Severity, TreatmentSite> ValidCombos =
        [
            (Guid.CreateVersion7(), Severity.Mild, TreatmentSite.Outpatient),          // both lower boundaries
            (Guid.CreateVersion7(), Severity.Mild, TreatmentSite.IntensiveCareUnit),
            (Guid.CreateVersion7(), Severity.Severe, TreatmentSite.Outpatient),
            (Guid.CreateVersion7(), Severity.Severe, TreatmentSite.IntensiveCareUnit), // both upper boundaries
            (Guid.CreateVersion7(), Severity.Moderate, TreatmentSite.Inpatient),       // middle values
        ];

        [Theory]
        [MemberData(nameof(ValidCombos))]
        public async Task UpdateCause_Success(Guid pathogenId, Severity severity, TreatmentSite treatmentSite)
        {
            var result = await _validator.ValidateAsync(new UpdateSuspectedCauseCommand
            {
                Id = Guid.CreateVersion7(),
                PathogenId = pathogenId,
                Severity = severity,
                TreatmentSite = treatmentSite,
            }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        public static readonly TheoryData<Guid, Guid, Severity, TreatmentSite, string> InvalidCommands =
        [
            // Boundary: empty GUID violates NotEmpty on Id
            (Guid.Empty, Guid.CreateVersion7(), Severity.Mild, TreatmentSite.Outpatient, "Id"),
            (Guid.CreateVersion7(), Guid.Empty, Severity.Mild, TreatmentSite.Outpatient, "PathogenId"),
            // Invalid enum values are produced by casting an out-of-range integer
            (Guid.CreateVersion7(), Guid.CreateVersion7(), (Severity)999, TreatmentSite.Outpatient, "Severity"),
            (Guid.CreateVersion7(), Guid.CreateVersion7(), Severity.Mild, (TreatmentSite)999, "TreatmentSite"),
        ];

        [Theory]
        [MemberData(nameof(InvalidCommands))]
        public async Task UpdateCause_Fail(Guid id, Guid pathogenId, Severity severity, TreatmentSite treatmentSite,
            string property)
        {
            var result = await _validator.ValidateAsync(new UpdateSuspectedCauseCommand
            {
                Id = id,
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
