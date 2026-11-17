using Respira.Patient.Application.Features.Patients.DischargePatient;
using Respira.Patient.Domain.Enums;

namespace Respira.Patient.Application.Test.Features.Patients.DischargePatient
{
    public class DischargePatientValidatorTest
    {
        private readonly DischargePatientValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task DischargePatient_RealisticCommand_Success()
        {
            // A discharge order: the patient's ID plus the recorded outcome of the
            // episode (UUID v7 as produced by the system for every new patient)
            var result = await _validator.ValidateAsync(
                ValidCommand(), TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary: a UUID v7 is the usual shape, plus the lowest and highest
        // non-empty GUID values that NotEmpty still accepts
        public static readonly TheoryData<Guid> ValidIds =
        [
            Guid.CreateVersion7(),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Theory]
        [MemberData(nameof(ValidIds))]
        public async Task DischargePatient_ValidId_Success(Guid id)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = id }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Every member defined by the TreatmentOutcome enum is a legitimate
        // discharge outcome - the handler doc goes out of its way to state that
        // even OutOfScope counts as a discharge in this system
        [Theory]
        [InlineData(TreatmentOutcome.InTreatment)]
        [InlineData(TreatmentOutcome.Recovered)]
        [InlineData(TreatmentOutcome.Death)]
        [InlineData(TreatmentOutcome.OutOfScope)]
        public async Task DischargePatient_DefinedOutcome_Success(TreatmentOutcome outcome)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Outcome = outcome }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
        [Fact]
        public async Task DischargePatient_EmptyId_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = Guid.Empty }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        // Boundary: OutOfScope (3) is the last member, so 4 is one step past the
        // enum, and -1 one step below the first member (InTreatment, 0)
        [Theory]
        [InlineData((TreatmentOutcome)4)]
        [InlineData((TreatmentOutcome)(-1))]
        public async Task DischargePatient_UndefinedOutcome_Fail(TreatmentOutcome outcome)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Outcome = outcome }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Outcome", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task DischargePatient_AllFieldsInvalid_Fail()
        {
            // Boundary: every rule that can fail at once fails independently, so the
            // client learns about both mistakes in a single response
            var command = new DischargePatientCommand
            {
                Id = Guid.Empty, // boundary: the only rejected Guid
                Outcome = (TreatmentOutcome)4, // boundary: one step past the last member
            };

            var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "Id");
            Assert.Contains(result.Errors, x => x.PropertyName == "Outcome");
        }

        #endregion

        private static DischargePatientCommand ValidCommand() => new()
        {
            Id = Guid.CreateVersion7(),
            Outcome = TreatmentOutcome.Recovered,
        };
    }
}
