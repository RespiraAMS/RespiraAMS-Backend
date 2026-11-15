using FluentValidation;

namespace Respira.Authentication.Application.Features.Accounts.Commands.ChangeStatus
{
    /// <summary>
    /// Validates the command to change an account's status.
    /// </summary>
    public class ChangeAccountStatusValidator : AbstractValidator<ChangeAccountStatusCommand>
    {
        public ChangeAccountStatusValidator()
        {
            RuleFor(x => x.TargetAccountId)
                .NotEmpty()
                .WithMessage("Target account ID is required.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid account status.");
        }
    }
}
