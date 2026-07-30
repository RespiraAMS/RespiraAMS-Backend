namespace Application.Features.Diseases.GetDiseaseCriteria;

public class GetDiseaseCriteriaValidator : AbstractValidator<GetDiseaseCriteriaQuery>
{
    public GetDiseaseCriteriaValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Disease ID is required");
    }
}