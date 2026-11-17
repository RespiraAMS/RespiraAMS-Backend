using Respira.Clinical.Application.Features.Criteria.GetPagedCriterion;
using Respira.Clinical.Application.Features.Criteria.UpdateCriterion;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedCriterionRequestDto
    {
        /// <summary>
        /// Pagination parameter: page index (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Pagination parameter: page size
        /// </summary>
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
        /// <summary>
        /// Criterion name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Criterion formula
        /// </summary>
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
