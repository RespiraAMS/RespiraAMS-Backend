using Respira.ServiceDefaults.Extensions;

namespace Application.Features.Antibiotics.GetPagedAntibiotic;

public class GetPagedAntibioticValidator : AbstractValidator<GetPagedAntibioticQuery>
{
    public GetPagedAntibioticValidator()
    {
        RuleFor(x => x.Param).IsValidPaginationParam();
    }
}