using Respira.ServiceDefaults.Extensions;

namespace Application.Features.Pathogens.GetPagedPathogen;

public class GetPagedPathogensValidator : AbstractValidator<GetPagedPathogenQuery>
{
    public GetPagedPathogensValidator()
    {
        RuleFor(x => x.Param).IsValidPaginationParam();
    }
}