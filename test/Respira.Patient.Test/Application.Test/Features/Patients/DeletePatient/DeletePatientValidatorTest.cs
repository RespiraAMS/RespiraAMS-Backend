using Respira.Patient.Application.Features.Patients.DeletePatient;

namespace Respira.Patient.Application.Test.Features.Patients.DeletePatient
{
    public class DeletePatientValidatorTest
    {
        private readonly DeletePatientValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task DeletePatient_RealisticCommand_Success()
        {
            // The command only carries the ID of the patient to remove, a UUID v7
            // as produced by the system for every new patient
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
        public async Task DeletePatient_ValidId_Success(Guid id)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = id }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
        [Fact]
        public async Task DeletePatient_EmptyId_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = Guid.Empty }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        #endregion

        private static DeletePatientCommand ValidCommand() => new()
        {
            Id = Guid.CreateVersion7(),
        };
    }
}
