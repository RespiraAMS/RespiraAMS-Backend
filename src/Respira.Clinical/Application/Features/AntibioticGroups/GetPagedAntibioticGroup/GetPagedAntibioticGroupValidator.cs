using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.AntibioticGroups.GetPagedAntibioticGroup
{
    public class GetPagedAntibioticGroupValidator : AbstractValidator<GetPagedAntibioticGroupQuery>
    {
        public GetPagedAntibioticGroupValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}
