using FluentValidation;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariableById
{
    public class GetClinicalVariableByIdValidator : AbstractValidator<GetClinicalVariableByIdQuery>
    {
        public GetClinicalVariableByIdValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Clinical variable ID must be provided");
        }
    }
}
