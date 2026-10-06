using System.Text.Json;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Shared.ManageFormula
{
    public class FormulaMapper : IMapper<FormulaDto, Formula>
    {
        /// <summary>
        /// Since we need the list of variables to construct the formula, dependencies
        /// must be passed in as List<ClinicalVariable>
        /// </summary>
        /// <param name="source">DTO object</param>
        /// <param name="dependencies">List of clinical variables</param>
        /// <returns>Formula object</returns>
        public Formula Map(FormulaDto source, object? dependencies = null)
        {
            if (dependencies is List<ClinicalVariable> variables)
            {
                // Resolve constant 
                if (source.Constant is not null)
                {
                    if (bool.TryParse(source.Constant, out var boolValue))
                    {
                        return new BooleanConstantFormula(boolValue);
                    }

                    if (decimal.TryParse(source.Constant, out var decimalValue))
                    {
                        return new NumericConstantFormula(decimalValue);
                    }

                    // Empty string validation is in validation layer, not here
                    return new CategoricalConstantFormula(source.Constant);
                }

                // Resolve variable
                if (source.Variable is not null)
                {
                    var variable = variables.FirstOrDefault(v => v.Id == source.Variable);
                    return variable is null
                        ? throw new ArgumentException($"Variable with id {source.Variable} not found")
                        : (Formula)new VariableFormula(variable);
                }

                // Resolve binary formula 
                if (source.Operator is not null && source.Left is not null && source.Right is not null)
                {
                    var left = Map(source.Left, variables);
                    var right = Map(source.Right, variables);
                    return new BinaryFormula(left, right, source.Operator.Value);
                }

                // Resolve unary formula
                if (source.Operand is not null)
                {
                    var operand = Map(source.Operand, variables);
                    return new UnaryFormula(operand);
                }

                // Resolve ternary formula
                if (source.Condition is not null && source.IfTrue is not null && source.IfFalse is not null)
                {
                    var condition = Map(source.Condition, variables);
                    var ifTrue = Map(source.IfTrue, variables);
                    var ifFalse = Map(source.IfFalse, variables);
                    return new TernaryFormula(condition, ifTrue, ifFalse);
                }

                throw new ArgumentException($"Invalid formula DTO: unable to determine formula type: {JsonSerializer.Serialize(source)}");
            }

            throw new ArgumentException("Unable to construct formula: dependencies must be convertable to List<ClinicalVariable>");
        }

        Result<Formula> IMapper<FormulaDto, Formula>.Map(FormulaDto source, object? dependencies)
        {
            if (dependencies is List<ClinicalVariable> variables)
            {
                // Resolve constant 
                if (source.Constant is not null)
                {
                    if (bool.TryParse(source.Constant, out var boolValue))
                    {
                        return Result<Formula>.Success(ApplicationStatus.Success, new BooleanConstantFormula(boolValue));
                    }

                    if (decimal.TryParse(source.Constant, out var decimalValue))
                    {
                        return Result<Formula>.Success(ApplicationStatus.Success, new NumericConstantFormula(decimalValue));
                    }

                    // Empty string validation is in validation layer, not here
                    return Result<Formula>.Success(ApplicationStatus.Success, new CategoricalConstantFormula(source.Constant));
                }

                // Resolve variable
                if (source.Variable is not null)
                {
                    var variable = variables.FirstOrDefault(v => v.Id == source.Variable);

                    return variable is null
                        ? Result<Formula>.Failure(new Error(
                            ApplicationStatus.BadRequest,
                            "Clinical variable with this ID not found",
                            new { source.Variable }))
                        : Result<Formula>.Success(ApplicationStatus.Success, new VariableFormula(variable));
                }

                // Resolve binary formula 
                if (source.Operator is not null && source.Left is not null && source.Right is not null)
                {
                    var left = Map(source.Left, variables);
                    var right = Map(source.Right, variables);
                    return Result<Formula>.Success(ApplicationStatus.Success, new BinaryFormula(left, right, source.Operator.Value));
                }

                // Resolve unary formula
                if (source.Operand is not null)
                {
                    var operand = Map(source.Operand, variables);
                    return Result<Formula>.Success(ApplicationStatus.Success, new UnaryFormula(operand));
                }

                // Resolve ternary formula
                if (source.Condition is not null && source.IfTrue is not null && source.IfFalse is not null)
                {
                    var condition = Map(source.Condition, variables);
                    var ifTrue = Map(source.IfTrue, variables);
                    var ifFalse = Map(source.IfFalse, variables);
                    return Result<Formula>.Success(ApplicationStatus.Success, new TernaryFormula(condition, ifTrue, ifFalse));
                }

                return Result<Formula>.Failure(new Error(
                    ApplicationStatus.BadRequest,
                    "Invalid formula DTO: unable to determine formula type",
                    new { source }));
            }

            return Result<Formula>.Failure(new Error(
                ApplicationStatus.BadRequest,
                "Unable to construct formula: dependencies must be convertable to List<ClinicalVariable>",
                new { dependencies }));
        }
    }
}
