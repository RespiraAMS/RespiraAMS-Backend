using FluentValidation;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine.Attributes;

namespace Respira.Patient.Application.Features.TreatmentPlans.UpsertTreatmentPlan
{
    /// <summary>
    /// Catches the <see cref="ValidationException"/> thrown by Wolverine's FluentValidation
    /// middleware and turns it into a normal failure <see cref="Result{T}"/> response.
    /// <para>
    /// Without this, the exception escapes the message handler. The command is invoked by the
    /// clinical service with request/reply, so no reply is produced and the caller waits until
    /// its 5 second remote invocation timeout expires.
    /// </para>
    /// </summary>
    public static class UpsertTreatmentPlanMiddleware
    {
        [WolverineOnException]
        public static Result<UpsertTreatmentPlanResult> OnException(
            ValidationException exception)
        {
            var message = string.Join(
                "; ",
                exception.Errors.Select(failure => failure.ErrorMessage));

            if (string.IsNullOrWhiteSpace(message))
            {
                message = exception.Message;
            }

            return Result<UpsertTreatmentPlanResult>.Failure(
                new ApplicationError(ApplicationStatus.BadRequest, message));
        }
    }
}
