using Respira.Patient.Application.Features.Patients.DeleteNextOfKin;

namespace Respira.Patient.Application.Test.Features.Patients.DeleteNextOfKin
{
    public class DeleteNextOfKinValidatorTest
    {
        private readonly DeleteNextOfKinValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task DeleteNextOfKin_RealisticCommand_Success()
        {
            // The command only carries the ID of the next of kin to remove, a UUID
            // v7 as produced by the system for every new record
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
        public async Task DeleteNextOfKin_ValidId_Success(Guid id)
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = id }, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
        [Fact]
        public async Task DeleteNextOfKin_EmptyId_Fail()
        {
            var result = await _validator.ValidateAsync(
                ValidCommand() with { Id = Guid.Empty }, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        #endregion

        private static DeleteNextOfKinCommand ValidCommand() => new()
        {
            Id = Guid.CreateVersion7(),
        };
    }
}
