using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetPagedClinicalVariable
{
    public class GetPagedClinicalVariableValidator : AbstractValidator<GetPagedClinicalVariableQuery>
    {
        public GetPagedClinicalVariableValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}
