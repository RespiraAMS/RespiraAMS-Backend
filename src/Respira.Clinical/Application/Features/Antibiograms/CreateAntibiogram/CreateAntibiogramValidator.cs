using FluentValidation;

namespace Respira.Clinical.Application.Features.Antibiograms.CreateAntibiogram
{
    public class CreateAntibiogramValidator : AbstractValidator<CreateAntibiogramCommand>
    {
        public CreateAntibiogramValidator()
        {
            RuleFor(x => x.PathogenId).NotEmpty().WithMessage("Pathogen ID is required");
            RuleFor(x => x.MicCategory).IsInEnum().WithMessage("MIC category is required and must be valid");
            RuleFor(x => x.AntibioticIds).NotEmpty().WithMessage("Antibiotic list must not be empty");
            RuleFor(x => x.AntibioticIds)
                .Must(x => x.Count == x.Distinct().Count())
                .WithMessage("Antibiotic ID list must not contain duplicate");
            RuleForEach(x => x.AntibioticIds)
                .NotEmpty()
                .WithMessage("Antibiotic ID is required and not empty");
        }
    }
}
