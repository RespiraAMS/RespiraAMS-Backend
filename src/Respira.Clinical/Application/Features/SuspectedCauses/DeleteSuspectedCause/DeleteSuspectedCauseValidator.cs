using FluentValidation;

namespace Respira.Clinical.Application.Features.SuspectedCauses.DeleteSuspectedCause
{
    public class DeleteSuspectedCauseValidator : AbstractValidator<DeleteSuspectedCauseCommand>
    {
        public DeleteSuspectedCauseValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("ID is required");
        }
    }
}
