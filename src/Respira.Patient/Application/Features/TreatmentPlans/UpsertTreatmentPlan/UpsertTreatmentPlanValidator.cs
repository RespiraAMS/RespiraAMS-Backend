using FluentValidation;
using JasperFx.Core;
using Respira.Patient.Domain.Models;

namespace Respira.Patient.Application.Features.TreatmentPlans.UpsertTreatmentPlan
{
    public class AntibioticValidator : AbstractValidator<Antibiotic>
    {
        public AntibioticValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Antibiotic ID is required");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Antibiotic name is required");
            RuleFor(x => x.Classification).NotEmpty().WithMessage("Antibiotic classification is required");
        }
    }

    public class UpsertTreatmentPlanValidator : AbstractValidator<UpsertTreatmentPlanCommand>
    {
        public UpsertTreatmentPlanValidator()
        {
            RuleFor(x => x.DoctorId).NotEmpty().WithMessage("Doctor ID is required");
            RuleFor(x => x.PatientId).NotEmpty().WithMessage("Patient ID is required");
            RuleFor(x => x.DiagnosisId).NotEmpty().WithMessage("Diagnsis ID is required");
            RuleFor(x => x.Diagnosis).NotEmpty().WithMessage("Diagnosis is required");
            RuleFor(x => x.Medicines).NotEmpty().WithMessage("Medicines must not be empty");
            RuleForEach(x => x.Medicines).SetValidator(new AntibioticValidator());
            RuleFor(x => x)
                .Must(x =>
                {
                    if (x.IsAdhere)
                    {
                        return x.ReasonForMedicineChoice is null;
                    }

                    return x.ReasonForMedicineChoice?.Trim().IsNotEmpty() == true;

                })
                .WithMessage("Reason for medicines choice must either be null (IsAdhere = true) or non empty string (IsAdhere = false)");

        }
    }
}
