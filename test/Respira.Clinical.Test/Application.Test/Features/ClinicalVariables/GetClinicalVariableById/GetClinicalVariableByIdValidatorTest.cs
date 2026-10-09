using Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariableById;

namespace Respira.Application.Test.Features.ClinicalVariables.GetClinicalVariableById
{
    public class GetClinicalVariableByIdValidatorTest
    {
        private readonly GetClinicalVariableByIdValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task GetClinicalVariableById_Success()
        {
            // Boundary: a UUID v7 is the smallest shape of a valid non-empty id
            var result = await _validator.ValidateAsync(
                new GetClinicalVariableByIdQuery { Id = Guid.CreateVersion7() },
                TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        [Fact]
        public async Task GetClinicalVariableById_EmptyId_Fail()
        {
            // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
            var result = await _validator.ValidateAsync(
                new GetClinicalVariableByIdQuery { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        #endregion
    }
}
