using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Doctor.Application.Features.Doctors.Queries.SearchDoctor
{
    public class SearchDoctorValidator : AbstractValidator<SearchDoctorQuery>
    {
        public SearchDoctorValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
            RuleFor(x => x.SearchTerm).IsValidSearchTerm();
        }
    }
}
