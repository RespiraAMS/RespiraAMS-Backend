using FluentValidation;

namespace Application.Features.Authentication.Login.Queries
{
    /// <summary>
    /// Validates login credentials: email format and non-empty password
    /// </summary>
    public class LoginQueryValidator : AbstractValidator<LoginQuery>
    {
        public LoginQueryValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);
            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }
}