namespace Application.Features.Diseases.UpdateDisease;

public class UpdateDiseaseValidator : AbstractValidator<UpdateDiseaseCommand>
{
    public UpdateDiseaseValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Disease ID is required");
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Disease name is required");
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Disease description is required");
        RuleFor(x => x.IcuScoreThreshold)
            .GreaterThan(0)
            .WithMessage("Disease ICU hospitalization score threshold must be a positive integer");
    }
}