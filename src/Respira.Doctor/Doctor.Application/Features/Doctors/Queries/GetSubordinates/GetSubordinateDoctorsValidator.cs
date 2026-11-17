using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetSubordinates
{
    public class GetSubordinateDoctorsValidator : AbstractValidator<GetSubordinateDoctorsQuery>
    {
        public GetSubordinateDoctorsValidator()
        {
            RuleFor(x => x.HeadDoctorAccountId)
                .NotEmpty()
                .WithMessage("Head doctor account identifier is required");

            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}
