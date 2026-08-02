namespace Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

public class DeleteIcuHospitalizeCriterionValidator : AbstractValidator<DeleteIcuHospitalizeCriterionCommand>
{
    public DeleteIcuHospitalizeCriterionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Disease's ICU hospitalize criterion ID is required");
    }
}