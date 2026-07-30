using Respira.ServiceDefaults.Extensions;

namespace Application.Features.Diseases.GetPagedDisease;

public class GetPagedDiseaseValidator : AbstractValidator<GetPagedDiseaseQuery>
{
    public GetPagedDiseaseValidator()
    {
        RuleFor(x => x.Param).IsValidPaginationParam();
    }
}