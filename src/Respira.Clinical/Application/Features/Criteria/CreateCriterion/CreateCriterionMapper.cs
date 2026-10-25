using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.Criteria.CreateCriterion
{
    public class CreateCriterionMapper(IMapper<FormulaDto, Formula> formulaMapper)
        : ICreateMapper<CreateCriterionCommand, Criterion>
    {
        public Result<Criterion> ToModel(CreateCriterionCommand command)
        {
            throw new NotImplementedException("This method won't be implemented since it required external dependencies to work");
        }

        public Result<Criterion> ToModel(CreateCriterionCommand command, object dependencies)
        {
            var variables = dependencies as List<ClinicalVariable>;
            var formulaMapResult = formulaMapper.Map(command.Formula, variables);
            if (formulaMapResult.IsFailure())
            {
                return Result<Criterion>.Failure(formulaMapResult.Error!);
            }

            // Since the Criterion enforce the rule that, a criterion formula must evaluate to a boolean value,
            // which we cannot check in the handler, so we will handle this case in the mapper
            if (formulaMapResult.Data!.ResultType != ExpressionResultType.Boolean)
            {
                return Result<Criterion>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "Formula result type should be boolean"));
            }

            return Result<Criterion>.Success(ApplicationStatus.Success, new Criterion
            {
                Name = command.Name,
                Formula = formulaMapResult.Data!,
            });
        }
    }
}
