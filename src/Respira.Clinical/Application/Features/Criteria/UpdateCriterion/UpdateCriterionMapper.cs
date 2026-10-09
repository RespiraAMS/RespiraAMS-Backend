using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Criteria.UpdateCriterion
{
    public class UpdateCriterionMapper(IMapper<FormulaDto, Formula> formulaMapper)
        : IUpdateMapper<Criterion, UpdateCriterionCommand>
    {
        public Result MapModel(Criterion model, UpdateCriterionCommand command)
        {
            throw new NotImplementedException();
        }

        public Result MapModel(Criterion model, UpdateCriterionCommand command, object? dependencies = null)
        {
            var variables = dependencies as List<ClinicalVariable>;
            var formulaMapResult = formulaMapper.Map(command.Formula, variables);
            if (formulaMapResult.IsFailure())
            {
                return Result.Failure(formulaMapResult.Error!);
            }

            var formula = formulaMapResult.Data!;
            if (formula.ResultType != ExpressionResultType.Boolean)
            {
                return Result.Failure(new Error(ApplicationStatus.BadRequest, "Formula result type should be boolean"));
            }

            model.Name = command.Name;
            model.Formula = formula;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }
    }
}
