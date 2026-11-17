using FluentValidation;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Application.Features.Diagnosis.Diagnose
{
    public class ObservationQueryValidator : AbstractValidator<ObservationQuery>
    {

        public ObservationQueryValidator()
        {
            RuleFor(x => x.VariableId).NotEmpty().WithMessage("Clinical variable is required");
            RuleFor(x => x.Value).NotEmpty().WithMessage("Clinical variable value is required");
        }
    }

    public class AllergyQueryValidator : AbstractValidator<AllergyQuery>
    {

        public AllergyQueryValidator()
        {
            RuleFor(x => x.Type).IsInEnum().WithMessage("Allergy type is required and must be valid");
            RuleForEach(x => x.AllergicAntibioticIds)
                .NotEmpty()
                .When(x => x.AllergicAntibioticIds.Any())
                .WithMessage("Allergic antibiotic IDs must not be empty UUID");
            RuleFor(x => x.OtherAllergy)
                .Must(x => x is null || x.Trim() != string.Empty)
                .WithMessage("Other allergy cannot be empty string");
            RuleFor(x => x)
                 .Must(x =>
                 {
                     return x.Type switch
                     {
                         AllergyType.No => !x.AllergicAntibioticIds.Any() && x.OtherAllergy is null,
                         AllergyType.Yes => x.AllergicAntibioticIds.Any() && x.OtherAllergy is null,
                         _ => !x.AllergicAntibioticIds.Any() && x.OtherAllergy is not null
                     };
                 })
             .WithMessage("Allergy was given value that semantically conflicted");
        }
    }

    public class MicrobialSampeQueryValidator : AbstractValidator<MicrobialSampleQuery>
    {
        public MicrobialSampeQueryValidator()
        {
            RuleFor(x => x.PathogenId).NotEmpty().WithMessage("Pathogen ID is required");
            RuleFor(x => x.MicCategory).IsInEnum().WithMessage("MIC category is required and must be valid");
            RuleFor(x => x.SampleType).IsInEnum().WithMessage("Sample type is required and must be valid");
        }
    }


    public class DiagnoseValidator : AbstractValidator<DiagnoseQuery>
    {
        public DiagnoseValidator()
        {

            RuleFor(x => x.Allergy).SetValidator(new AllergyQueryValidator());
            RuleForEach(x => x.MicrobialSamples).SetValidator(new MicrobialSampeQueryValidator());
            RuleForEach(x => x.Observations).SetValidator(new ObservationQueryValidator());
            RuleForEach(x => x.OtherInfections)
                .IsInEnum()
                .When(x => x.OtherInfections.Any())
                .WithMessage("Other infection is required and must be valid");
            RuleForEach(x => x.SpecialMedicalHistory)
                .NotEmpty()
                .When(x => x.SpecialMedicalHistory.Any())
                .WithMessage("Special medical history must not be empty string");
        }
    }
}
