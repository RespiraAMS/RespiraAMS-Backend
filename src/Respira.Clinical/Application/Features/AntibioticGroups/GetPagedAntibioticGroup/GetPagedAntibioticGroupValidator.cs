using Respira.ServiceDefaults.Extensions;

namespace Application.Features.AntibioticGroups.GetPagedAntibioticGroup;

public class GetPagedAntibioticGroupValidator : AbstractValidator<GetPagedAntibioticGroupQuery>
{
    public GetPagedAntibioticGroupValidator()
    {
        RuleFor(x => x.Param).IsValidPaginationParam();
    }
}