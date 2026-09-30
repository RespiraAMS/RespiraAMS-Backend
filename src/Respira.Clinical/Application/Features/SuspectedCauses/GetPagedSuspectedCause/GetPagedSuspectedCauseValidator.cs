using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.SuspectedCauses.GetPagedSuspectedCause
{
    public class GetPagedSuspectedCauseValidator : AbstractValidator<GetPagedSuspectedCauseQuery>
    {
        public GetPagedSuspectedCauseValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}
