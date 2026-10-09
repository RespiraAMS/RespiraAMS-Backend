using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalVariables.CreateClinicalVariable
{
    public class CreateClinicalVariableMapper(IMapper<FormulaDto, Formula> formulaMapper)
        : ICreateMapper<CreateClinicalVariableCommand, ClinicalVariable>
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

        /// <summary>
        /// Because of the recursive structure of Formula,
        /// dependencies is required for this mapper, so this method
        /// will never be called. This is just for the interface implementation
        /// </summary>
        public Result<ClinicalVariable> ToModel(CreateClinicalVariableCommand command)
        {
            throw new NotImplementedException();
        }

        public Result<ClinicalVariable> ToModel(CreateClinicalVariableCommand command, object? dependencies = null)
        {
            // Map prerequisite first
            var variables = dependencies as List<ClinicalVariable> ?? throw new ArgumentException("Dependencies must be a list of clinical variables");
            var mapPreresiqiteResult = MapPrerequisite(command.Prerequisite, variables);
            if (mapPreresiqiteResult.IsFailure())
            {
                return Result<ClinicalVariable>.Failure(mapPreresiqiteResult.Error!);
            }
            var prerequisite = mapPreresiqiteResult.Data!;

            // Create clinical variable based on variable type
            return command.ValueType switch
            {
                ClinicalValueType.Boolean => Result<ClinicalVariable>.Success(ApplicationStatus.Success, new BooleanClinicalVariable
                {
                    Code = command.Code,
                    Name = command.Name,
                    Description = command.Description,
                    CanonicalUnit = command.CanonicalUnit,
                    IsRequired = command.IsRequired,
                    Category = command.Category,
                    Prerequisite = prerequisite,
                }),
                ClinicalValueType.Numeric => Result<ClinicalVariable>.Success(ApplicationStatus.Success, new NumericClinicalVariable
                {
                    Code = command.Code,
                    Name = command.Name,
                    Description = command.Description,
                    CanonicalUnit = command.CanonicalUnit,
                    IsRequired = command.IsRequired,
                    Category = command.Category,
                    Prerequisite = prerequisite,
                    AcceptedRange = command.AcceptedRange ?? throw new ArgumentException("Numeric clinical variable must have accepted range"),
                    NormalRange = command.NormalRange,
                }),
                ClinicalValueType.Categorical => Result<ClinicalVariable>.Success(ApplicationStatus.Success, new CategoricalClinicalVariable(command.AcceptedValues)
                {
                    Code = command.Code,
                    Name = command.Name,
                    Description = command.Description,
                    CanonicalUnit = command.CanonicalUnit,
                    IsRequired = command.IsRequired,
                    Category = command.Category,
                    Prerequisite = prerequisite,
                }),
                // Since the variable type is an enum, so adding an unexpected value is an exception,
                // not expected failure behavior, so we throw exception here instead of result failure
                _ => throw new ArgumentException($"Invalid clinical variable type: {command.ValueType}"),
            };
        }
    }
}
