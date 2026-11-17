using FluentValidation;

namespace Respira.Clinical.Application.Features.Diagnosis.WarnAntibioticCombination
{
    public class WarnAntibioticCombinationValidator : AbstractValidator<WarnAntibioticCombinationQuery>
    {
        public WarnAntibioticCombinationValidator()
        {
            RuleFor(x => x.AntibioticIds)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Medicine list cannot be empty")
                .Must(m => m.Distinct().Count() == m.Count)
                .WithMessage("Antibiotic IDs must be distinct");
        }
    }
}
