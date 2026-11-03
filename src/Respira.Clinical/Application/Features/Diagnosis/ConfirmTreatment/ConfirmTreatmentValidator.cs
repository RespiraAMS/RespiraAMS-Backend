using System.Text.Json;
using FluentValidation;

namespace Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment
{
    public class ConfirmTreatmentValidator : AbstractValidator<ConfirmTreatmentQuery>
    {
        public ConfirmTreatmentValidator()
        {
            RuleFor(x => x.DoctorId).NotEmpty().WithMessage("Doctor ID is required");
            RuleFor(x => x.PatientId).NotEmpty().WithMessage("Patient ID is required");

            // The diagnosis is raw JSON, so only its shape can be checked here.
            // Its content (diagnosis ID, protocols) is validated in the handler.
            RuleFor(x => x.Diagnosis)
                .Must(d => d.ValueKind == JsonValueKind.Object)
                .WithMessage("Diagnosis result is required");

            RuleFor(x => x.Medicines)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Medicine list cannot be empty")
                .Must(m => m.Distinct().Count() == m.Count)
                .WithMessage("Antibiotic IDs must be distinct");

            RuleForEach(x => x.Medicines).NotEmpty().WithMessage("Antibiotic ID is required");

            // Reason will not be validated here (see ConfirmTreatmentHandler)
        }
    }
}
