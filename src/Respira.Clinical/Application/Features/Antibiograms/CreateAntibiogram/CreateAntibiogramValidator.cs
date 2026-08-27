namespace Application.Features.Antibiograms.CreateAntibiogram;

public class CreateAntibiogramValidator : AbstractValidator<CreateAntibiogramCommand>
{
    public CreateAntibiogramValidator()
    {
        RuleFor(x => x.PathogenId)
            .NotEmpty()
            .WithMessage("Pathogen ID is required");
        RuleFor(x => x.MicLevel)
            .IsInEnum()
            .WithMessage("Mic level is invalid");
        RuleFor(x => x.MicIds)
            .NotEmpty()
            .WithMessage("Mic IDs is required");
        RuleForEach(x => x.MicIds)
            .NotEmpty()
            .WithMessage("Mic ID is required");
        RuleFor(x => x.FirstPriorityMedicineIds)
            .NotEmpty()
            .WithMessage("First priority medicine IDs is required");
        RuleForEach(x => x.FirstPriorityMedicineIds)
            .NotEmpty()
            .WithMessage("First priority medicine ID is required");
        RuleFor(x => x.SecondPriorityMedicineIds)
            .NotEmpty()
            .WithMessage("Second priority medicine IDs is required");
        RuleForEach(x => x.SecondPriorityMedicineIds)
            .NotEmpty()
            .WithMessage("Second priority medicine ID is required");
    }
}
