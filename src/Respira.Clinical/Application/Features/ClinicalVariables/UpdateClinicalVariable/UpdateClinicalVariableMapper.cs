using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalVariables.UpdateClinicalVariable
{
    public class UpdateClinicalVariableMapper(IMapper<FormulaDto, Formula> formulaMapper)
        : IUpdateMapper<ClinicalVariable, UpdateClinicalVariableCommand>
    {
        private Result<Formula?> MapPrerequisite(FormulaDto? dto, List<ClinicalVariable> variables)
        {
            Formula? prerequisite = null;

            if (dto is not null)
            {
                var mapResult = formulaMapper.Map(dto, variables);
                if (mapResult.IsFailure())
                {
                    return Result<Formula?>.Failure(mapResult.Error!);
                }
                prerequisite = mapResult.Data!;
            }

            return Result<Formula?>.Success(ApplicationStatus.Success, prerequisite);
        }

        public Result MapModel(ClinicalVariable model, UpdateClinicalVariableCommand command)
        {
            throw new NotImplementedException();
        }

        public Result MapModel(ClinicalVariable model, UpdateClinicalVariableCommand command, object? dependencies = null)
        {
            // Map prerequisite first
            var variables = dependencies as List<ClinicalVariable> ?? throw new ArgumentException("Dependencies must be a list of clinical variables");
            var mapPreresiqiteResult = MapPrerequisite(command.Prerequisite, variables);
            if (mapPreresiqiteResult.IsFailure())
            {
                return Result.Failure(mapPreresiqiteResult.Error!);
            }
            var prerequisite = mapPreresiqiteResult.Data!;

            // Create clinical variable based on variable type
            switch (model.ValueType) // Check on model instead of command
            {
                case ClinicalValueType.Boolean:
                    model.Name = command.Name;
                    model.Code = command.Code;
                    model.Description = command.Description;
                    model.CanonicalUnit = command.CanonicalUnit;
                    model.IsRequired = command.IsRequired;
                    model.Category = command.Category;
                    model.Prerequisite = prerequisite;
                    break;
                case ClinicalValueType.Numeric:
                    model.Name = command.Name;
                    model.Code = command.Code;
                    model.Description = command.Description;
                    model.CanonicalUnit = command.CanonicalUnit;
                    model.IsRequired = command.IsRequired;
                    model.Category = command.Category;
                    model.Prerequisite = prerequisite;
                    ((NumericClinicalVariable)model).AcceptedRange = command.AcceptedRange ?? throw new ArgumentException("Numeric clinical variable must have accepted range");
                    ((NumericClinicalVariable)model).NormalRange = command.NormalRange;
                    break;
                case ClinicalValueType.Categorical:
                    model.Name = command.Name;
                    model.Code = command.Code;
                    model.Description = command.Description;
                    model.CanonicalUnit = command.CanonicalUnit;
                    model.IsRequired = command.IsRequired;
                    model.Category = command.Category;
                    model.Prerequisite = prerequisite;
                    ((CategoricalClinicalVariable)model).AcceptedValues = command.AcceptedValues;
                    break;
                default:
                    throw new ArgumentException($"Invalid clinical variable type: {command.ValueType}");
            }

            return Result.Success(ApplicationStatus.Success);
        }
    }
}
