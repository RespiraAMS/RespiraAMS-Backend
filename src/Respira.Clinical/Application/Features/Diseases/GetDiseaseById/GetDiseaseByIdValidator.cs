namespace Application.Features.Diseases.GetDiseaseById;

public class GetDiseaseByIdValidator : AbstractValidator<GetDiseaseByIdQuery>
{
    public GetDiseaseByIdValidator()
    {
        RuleFor(q => q.Id).NotEmpty().WithMessage("Disease ID is required");
    }
}