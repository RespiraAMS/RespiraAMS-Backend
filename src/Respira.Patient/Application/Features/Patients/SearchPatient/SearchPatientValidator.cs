using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Patient.Application.Features.Patients.SearchPatient
{
    public class SearchPatientValidator : AbstractValidator<SearchPatientQuery>
    {
        public SearchPatientValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
            RuleFor(x => x.SearchTerm).IsValidSearchTerm();
        }
    }
}
