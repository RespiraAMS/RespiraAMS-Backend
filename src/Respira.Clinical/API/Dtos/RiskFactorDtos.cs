using Respira.Clinical.Application.Features.RiskFactors.GetPagedRiskFactor;
using Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedRiskFactorRequestDto
    {
        /// <summary>
        /// Pagination parameter: page index (1-based)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Pagination parameter: page size
        /// </summary>
        public int Size { get; set; } = 10;

        /// <summary>
        /// Pathogen ID
        /// </summary>
        public Guid? PathogenId { get; set; }

        public GetPagedRiskFactorQuery ToQuery()
        {
            return new GetPagedRiskFactorQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size
                },
                Filter = new RiskFactorFilter
                {
                    PathogenId = PathogenId
                }
            };
        }
    }

    public record UpdateRiskFactorRequestDto
    {
        /// <summary>
        /// Pathogen ID
        /// </summary>
        public required Guid PathogenId { get; set; }

        /// <summary>
        /// Risk factor criterion ID
        /// </summary>
        public required Guid CriterionId { get; set; }

        public UpdateRiskFactorCommand ToCommand(Guid id)
        {
            return new UpdateRiskFactorCommand
            {
                Id = id,
                PathogenId = PathogenId,
                CriterionId = CriterionId
            };
        }
    }
}
