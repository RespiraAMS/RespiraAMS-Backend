using FluentValidation;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetByAccountId
{
    public class GetDoctorByAccountIdValidator : AbstractValidator<GetDoctorByAccountIdQuery>
    {
        public GetDoctorByAccountIdValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty()
                .WithMessage("Account ID is required");
        }
    }
}
