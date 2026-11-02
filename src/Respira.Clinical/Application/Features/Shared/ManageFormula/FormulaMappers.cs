using System.Globalization;
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Shared.ManageFormula
{
    /// <summary>
    /// Map from <see cref= "FormulaDto" /> to <see cref= "Formula" />.
    /// </summary>
    public class FormulaDtoMapper : IMapper<FormulaDto, Formula>
    {
        public Result<Formula> Map(FormulaDto source, object? dependencies)
        {
            try
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

                        if (decimal.TryParse(source.Constant, CultureInfo.InvariantCulture, out var decimalValue))
                        {
                            return Result<Formula>.Success(ApplicationStatus.Success, new NumericConstantFormula(decimalValue));
                        }

                        return Result<Formula>.Success(ApplicationStatus.Success, new CategoricalConstantFormula(source.Constant));
                    }

                    // Resolve variable
                    if (source.Variable is not null)
                    {
                        var variable = variables.FirstOrDefault(v => v.Id == source.Variable);

                        return variable is null
                            ? Result<Formula>.Failure(new ApplicationError(
                                ApplicationStatus.BadRequest,
                                "Clinical variable with this ID not found",
                                new { source.Variable }))
                            : Result<Formula>.Success(ApplicationStatus.Success, new VariableFormula(variable));
                    }

                    // Resolve binary formula 
                    if (source.Operator is not null && source.Left is not null && source.Right is not null)
                    {
                        var mapLeftResult = Map(source.Left, variables);
                        if (mapLeftResult.IsFailure())
                        {
                            return Result<Formula>.Failure(mapLeftResult.Error!);
                        }
                        var left = mapLeftResult.Data!;

                        var mapRightResult = Map(source.Right, variables);
                        if (mapRightResult.IsFailure())
                        {
                            return Result<Formula>.Failure(mapRightResult.Error!);
                        }
                        var right = mapRightResult.Data!;

                        return Result<Formula>.Success(ApplicationStatus.Success, new BinaryFormula(left, right, source.Operator.Value));
                    }

                    // Resolve unary formula
                    if (source.Operand is not null)
                    {
                        var mapOperandResult = Map(source.Operand, variables);
                        if (mapOperandResult.IsFailure())
                        {
                            return Result<Formula>.Failure(mapOperandResult.Error!);
                        }
                        var operand = mapOperandResult.Data!;

                        return Result<Formula>.Success(ApplicationStatus.Success, new UnaryFormula(operand));
                    }

                    // Resolve ternary formula
                    if (source.Condition is not null && source.IfTrue is not null && source.IfFalse is not null)
                    {
                        var mapConditionResult = Map(source.Condition, variables);
                        if (mapConditionResult.IsFailure())
                        {
                            return Result<Formula>.Failure(mapConditionResult.Error!);
                        }
                        var condition = mapConditionResult.Data!;

                        var mapIfTrueResult = Map(source.IfTrue, variables);
                        if (mapIfTrueResult.IsFailure())
                        {
                            return Result<Formula>.Failure(mapIfTrueResult.Error!);
                        }
                        var ifTrue = mapIfTrueResult.Data!;

                        var mapIfFalseResult = Map(source.IfFalse, variables);
                        if (mapIfFalseResult.IsFailure())
                        {
                            return Result<Formula>.Failure(mapIfFalseResult.Error!);
                        }
                        var ifFalse = mapIfFalseResult.Data!;

                        return Result<Formula>.Success(ApplicationStatus.Success, new TernaryFormula(condition, ifTrue, ifFalse));

                    }

                    return Result<Formula>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Invalid formula DTO: unable to determine formula type", new { source }));
                }

                return Result<Formula>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Unable to construct formula: dependencies must be convertable to List<ClinicalVariable>", new { dependencies }));

            }
            // Because the domain model throw ArgumentException if the formula properties are invalid
            // -> expected behavior, so we will catch it here and return failure result instead
            // of throwing an exception.
            catch (ArgumentException e)
            {
                return Result<Formula>.Failure(new ApplicationError(ApplicationStatus.BadRequest, e.Message));
            }
        }
    }

    /// <summary>
    /// Map from <see cref= "Formula" /> to <see cref= "FormulaDto" />.
    /// </summary>
    public class FormulaModelMapper : IMapper<Formula, FormulaDto>
    {
        public Result<FormulaDto> Map(Formula source, object? dependencies = null)
        {
            if (source is BooleanConstantFormula booleanFormula)
            {
                return Result<FormulaDto>.Success(ApplicationStatus.Success, new FormulaDto
                {
                    Constant = booleanFormula.Constant.ToString()
                });
            }

            if (source is NumericConstantFormula numericFormula)
            {
                return Result<FormulaDto>.Success(ApplicationStatus.Success, new FormulaDto
                {
                    Constant = numericFormula.Constant.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (source is CategoricalConstantFormula categoricalFormula)
            {
                return Result<FormulaDto>.Success(ApplicationStatus.Success, new FormulaDto
                {
                    Constant = categoricalFormula.Constant
                });
            }

            if (source is VariableFormula variableFormula)
            {
                return Result<FormulaDto>.Success(ApplicationStatus.Success, new FormulaDto
                {
                    Variable = variableFormula.Variable.Id
                });
            }

            if (source is BinaryFormula binaryFormula)
            {
                var mapLeftResult = Map(binaryFormula.Left);
                if (mapLeftResult.IsFailure())
                {
                    return Result<FormulaDto>.Failure(mapLeftResult.Error!);
                }

                var mapRightResult = Map(binaryFormula.Right);
                if (mapRightResult.IsFailure())
                {
                    return Result<FormulaDto>.Failure(mapRightResult.Error!);
                }

                return Result<FormulaDto>.Success(ApplicationStatus.Success, new FormulaDto
                {
                    Operator = binaryFormula.Operator,
                    Left = mapLeftResult.Data!,
                    Right = mapRightResult.Data!
                });
            }

            if (source is UnaryFormula unaryFormula)
            {
                var mapOperandResult = Map(unaryFormula.Operand);
                if (mapOperandResult.IsFailure())
                {
                    return Result<FormulaDto>.Failure(mapOperandResult.Error!);
                }

                return Result<FormulaDto>.Success(ApplicationStatus.Success, new FormulaDto
                {
                    Operand = mapOperandResult.Data!
                });
            }

            if (source is TernaryFormula ternaryFormula)
            {
                var mapConditionResult = Map(ternaryFormula.Condition);
                if (mapConditionResult.IsFailure())
                {
                    return Result<FormulaDto>.Failure(mapConditionResult.Error!);
                }

                var mapIfTrueResult = Map(ternaryFormula.IfTrue);
                if (mapIfTrueResult.IsFailure())
                {
                    return Result<FormulaDto>.Failure(mapIfTrueResult.Error!);
                }

                var mapIfFalseResult = Map(ternaryFormula.IfFalse);
                if (mapIfFalseResult.IsFailure())
                {
                    return Result<FormulaDto>.Failure(mapIfFalseResult.Error!);
                }

                return Result<FormulaDto>.Success(ApplicationStatus.Success, new FormulaDto
                {
                    Operator = ExpressionOperator.Ternary,
                    Condition = mapConditionResult.Data!,
                    IfTrue = mapIfTrueResult.Data!,
                    IfFalse = mapIfFalseResult.Data!
                });
            }

            throw new ArgumentException("Unable to map formula to DTO: unknown formula type");
        }
    }
}
