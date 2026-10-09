using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetPagedClinicalVariable
{
    public record ClinicalVariableFilter
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public bool? IsRequired { get; set; }
        public ClinicalValueType? ValueType { get; set; }
        public ClinicalVariableCategory? Category { get; set; }
    }

    public record GetPagedClinicalVariableQuery : IQuery
    {
        public required PaginationParam Param { get; set; }
        public ClinicalVariableFilter? Filter { get; set; }
    }

    public record PagedClinicalVariableItem
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required bool IsRequired { get; set; }
        public string? CanonicalUnit { get; set; }
        public required ClinicalValueType ValueType { get; set; }
        public required ClinicalVariableCategory Category { get; set; }
    }
}
