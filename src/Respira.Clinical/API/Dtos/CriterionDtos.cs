using Respira.Clinical.Application.Features.Criteria.GetPagedCriterion;
using Respira.Clinical.Application.Features.Criteria.UpdateCriterion;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedCriterionRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;

        public GetPagedCriterionQuery ToQuery()
        {
            return new GetPagedCriterionQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size
                }
            };
        }
    }

    public record UpdateCriterionRequestDto
    {
        public required string Name { get; set; }
        public required FormulaDto Formula { get; set; }

        public UpdateCriterionCommand ToCommand(Guid id)
        {
            return new UpdateCriterionCommand
            {
                Id = id,
                Name = Name,
                Formula = Formula
            };
        }
    }
}
