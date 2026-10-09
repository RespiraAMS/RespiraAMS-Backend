using Respira.Clinical.Application.Features.RiskFactors.GetPagedRiskFactor;
using Respira.Clinical.Application.Features.RiskFactors.UpdateRiskFactor;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedRiskFactorRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
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
        public required Guid PathogenId { get; set; }
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
