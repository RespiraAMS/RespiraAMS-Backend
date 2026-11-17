using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalVariables.DeleteClinicalVariable
{
    public class DeleteClinicalVariableValidator : AbstractValidator<DeleteClinicalVariableCommand>
    {
        public DeleteClinicalVariableValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Clinical variable ID is required");
        }
    }
}
