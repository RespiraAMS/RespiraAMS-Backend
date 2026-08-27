namespace Application.Features.Antibiograms.UpdateAntibiogram;

public class UpdateAntibiogramValidator : AbstractValidator<UpdateAntibiogramCommand>
{
    public UpdateAntibiogramValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Antibiogram ID is required");
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
