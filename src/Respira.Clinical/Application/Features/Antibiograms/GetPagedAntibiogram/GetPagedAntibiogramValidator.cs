using Respira.ServiceDefaults.Extensions;

namespace Application.Features.Antibiograms.GetPagedAntibiogram;

public class GetPagedAntibiogramValidator : AbstractValidator<GetPagedAntibiogramQuery>
{
    public GetPagedAntibiogramValidator()
    {
        RuleFor(x => x.Param).IsValidPaginationParam();
    }
}