using Respira.ServiceDefaults.Extensions;

namespace Application.Features.Patients.GetPagedPatient;

public class GetPagedPatientValidator : AbstractValidator<GetPagedPatientQuery>
{
    public GetPagedPatientValidator()
    {
        RuleFor(x => x.Param).IsValidPaginationParam();
    }
}