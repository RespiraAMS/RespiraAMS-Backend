using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Patient.Application.Features.Patients.GetPagedPatient
{
    public class GetPagedPatientValidator : AbstractValidator<GetPagedPatientQuery>
    {
        public GetPagedPatientValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();

            // Rule for filter: if min age and max age are both set, then
            // min <= max age
            RuleFor(x => x.Filter)
                .Must(x => x!.MinAge <= x.MaxAge)
                .When(x => x.Filter?.MinAge is not null && x.Filter.MaxAge is not null);
        }
    }
}
