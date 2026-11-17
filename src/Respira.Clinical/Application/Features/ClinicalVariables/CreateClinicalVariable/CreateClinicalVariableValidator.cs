using FluentValidation;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Application.Features.Shared.ManageRange;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Application.Features.ClinicalVariables.CreateClinicalVariable
{
    public class CreateClinicalVariableValidator : AbstractValidator<CreateClinicalVariableCommand>
    {
        public CreateClinicalVariableValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Code is required");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required");
            RuleFor(x => x.ValueType).IsInEnum().WithMessage("Value type is required");
            RuleFor(x => x.Category).IsInEnum().WithMessage("Category is required");
            RuleFor(x => x.Prerequisite)
                .SetValidator(new FormulaValidator()!)
                .When(x => x.Prerequisite is not null);
            // Since accepted range is required when it's numeric clinical variable,
            // it must be not null when ValueType == Numeric
            RuleFor(x => x.AcceptedRange)
                .Must(x => x is not null)
                .When(x => x.ValueType == ClinicalValueType.Numeric)
                .WithMessage("Accepted range is required when value type is numeric");
            RuleFor(x => x.AcceptedRange)
                .SetValidator(new RangeValidator()!)
                .When(x => x.AcceptedRange is not null);
            // Normal range is optional when ValueType == Numeric,
            // so it value can only be not null when ValueType == Numeric
            RuleFor(x => x)
                .Must(x =>
                {
                    // Only when value type is numeric that normal range can be not null
                    if (x.NormalRange is not null)
                    {
                        return x.ValueType == ClinicalValueType.Numeric;
                    }

                    // If normal range is null, then it doesn't matter what value type is
                    return true;
                })
                .WithMessage("Only when value type is numeric that normal range can be not null");
            RuleFor(x => x.NormalRange)
                .SetValidator(new RangeValidator()!)
                .When(x => x.NormalRange is not null);
            RuleFor(x => x.AcceptedValues)
                    .NotEmpty()
                    .When(x => x.ValueType == ClinicalValueType.Categorical)
                    .WithMessage("Categorical values are required when value type is categorical");
            RuleForEach(x => x.AcceptedValues)
                    .NotEmpty()
                    .WithMessage("Value for categorical variables must not be empty string");

        }
    }
}
