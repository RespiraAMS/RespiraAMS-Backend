using FluentValidation;

namespace Respira.Patient.Application.Features.Patients.DeleteNextOfKin
{
    public class DeleteNextOfKinValidator : AbstractValidator<DeleteNextOfKinCommand>
    {
        public DeleteNextOfKinValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Next of kin ID is required");
        }
    }
}
