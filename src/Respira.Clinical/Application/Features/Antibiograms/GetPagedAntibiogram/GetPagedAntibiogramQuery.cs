using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.Antibiograms.GetPagedAntibiogram
{
    public record AntibiogramFilter
    {
        public Guid? PathogenId { get; set; }
        public MicCategory? MicCategory { get; set; }
    }

    public record GetPagedAntibiogramQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public AntibiogramFilter? Filter { get; set; }
    }

    public record AntibioticResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required Guid AntibioticGroupId { get; set; }
        public required string AntibioticGroupName { get; set; }
        public required AwareClassification Classification { get; set; }
    }

    public record PathogenResult(Guid Id, string Name);

    public record AntibiogramItem
    {

        public required Guid Id { get; set; }
        public required PathogenResult Pathogen { get; set; }
        public required MicCategory MicCategory { get; set; }
        public required IEnumerable<AntibioticResult> Antibiotics { get; set; }
    }

}
