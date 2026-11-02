using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor
{
    public class GetPagedDoctorValidator : AbstractValidator<GetPagedDoctorQuery>
    {
        public GetPagedDoctorValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}
