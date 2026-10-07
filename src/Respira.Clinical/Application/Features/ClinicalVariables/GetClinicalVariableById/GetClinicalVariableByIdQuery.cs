using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariableById
{
    public record GetClinicalVariableByIdQuery : IQuery
    {
        /// <summary>
        /// Clinical variable ID
        /// </summary>
        public required Guid Id { get; set; }
    }

    public record ClinicalVariableResult
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required ClinicalValueType ValueType { get; set; }
        public string? CanonicalUnit { get; set; }
        public required bool IsRequired { get; set; }
        public required ClinicalVariableCategory Category { get; set; }
        public string? Prerequisite { get; set; } // Store as string representation of Formula
        public Range? AcceptedRange { get; set; }
        public Range? NormalRange { get; set; }
        public List<string>? AcceptedValues { get; set; }
    }
}
