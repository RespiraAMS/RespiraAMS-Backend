using FluentValidation;
using FluentValidation.Results;
using Respira.Patient.Application.Features.TreatmentPlans.UpsertTreatmentPlan;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.TreatmentPlans.UpsertTreatmentPlan
{
    public class UpsertTreatmentPlanMiddlewareTest
    {
        #region Validation failure

        [Fact]
        public void UpsertTreatmentPlan_MultipleValidationFailures_ReturnsBadRequest()
        {
            // Every failure is combined into one readable error description so the
            // clinical service can show the complete reason to the doctor
            var exception = new ValidationException(
            [
                new ValidationFailure("DoctorId", "Doctor ID is required"),
                new ValidationFailure("Medicines", "Medicines must not be empty"),
            ]);

            var result = UpsertTreatmentPlanMiddleware.OnException(exception);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(ApplicationStatus.BadRequest, result.Error.StatusCode);
            Assert.Contains("Doctor ID is required", result.Error.Description);
            Assert.Contains("Medicines must not be empty", result.Error.Description);
        }

        [Fact]
        public void UpsertTreatmentPlan_SingleValidationFailure_ReturnsBadRequest()
        {
            var exception = new ValidationException(
            [
                new ValidationFailure(
                    string.Empty,
                    "Reason for medicines choice must either be null (IsAdhere = true) or non empty string (IsAdhere = false)"),
            ]);

            var result = UpsertTreatmentPlanMiddleware.OnException(exception);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(
                "Reason for medicines choice must either be null (IsAdhere = true) or non empty string (IsAdhere = false)",
                result.Error.Description);
        }

        #endregion

        #region Fallback message

        [Fact]
        public void UpsertTreatmentPlan_ExceptionWithoutFailures_UsesExceptionMessage()
        {
            // Boundary: a ValidationException built from a plain message has no
            // ValidationFailure entries, so the fallback message must be used
            var exception = new ValidationException("Validation failed");

            var result = UpsertTreatmentPlanMiddleware.OnException(exception);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(exception.Message, result.Error.Description);
        }

        // Boundary: a single failure with a blank message produces an empty joined
        // string, which must also fall back to the exception message
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\t")]
        public void UpsertTreatmentPlan_FailureWithBlankMessage_UsesExceptionMessage(string message)
        {
            var exception = new ValidationException(
            [
                new ValidationFailure("DoctorId", message),
            ]);

            var result = UpsertTreatmentPlanMiddleware.OnException(exception);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
            Assert.Equal(exception.Message, result.Error.Description);
        }

        #endregion
    }
}
