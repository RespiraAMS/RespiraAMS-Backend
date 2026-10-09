using Respira.Clinical.Application.Features.ClinicalVariables.GetPagedClinicalVariable;
using Respira.Clinical.Application.Features.ClinicalVariables.UpdateClinicalVariable;
using Respira.Clinical.Application.Features.Shared.ManageFormula;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedClinicalVariableRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string? Name { get; set; }
        public string? Code { get; set; }
        public bool? IsRequired { get; set; }
        public ClinicalValueType? ValueType { get; set; }
        public ClinicalVariableCategory? Category { get; set; }

        public GetPagedClinicalVariableQuery ToQuery()
        {
            return new GetPagedClinicalVariableQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size,
                },
                Filter = new ClinicalVariableFilter
                {
                    Name = Name,
                    Code = Code,
                    IsRequired = IsRequired,
                    ValueType = ValueType,
                    Category = Category
                }
            };
        }
    }

    public record UpdateClinicalVariableRequestDto
    {
        /// <summary>
        /// Clinical variable code. Preferably follow LOINC code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Clinical variable name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Clinical variable description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Clinical variable type. Note that update CANNOT change the value type,
        /// this value is simply needed for mapping and validation. This value must match
        /// exactly with the value type when created, or the API would fail
        /// </summary>
        public required ClinicalValueType ValueType { get; set; }

        /// <summary>
        /// Actual unit used by the engine
        /// </summary>
        public string? CanonicalUnit { get; set; }

        /// <summary>
        /// Clinical variable that is required when diagnosis or not
        /// </summary>
        public required bool IsRequired { get; set; }

        /// <summary>
        /// Clinical variable category
        /// </summary>
        public required ClinicalVariableCategory Category { get; set; }

        /// <summary>
        /// Prerequisite formula for this variable. For example,
        /// for a PREGNANT-OR-LACTATING variable to be true, then
        /// FEMALE must be true
        /// </summary>
        public FormulaDto? Prerequisite { get; set; }

        /// <summary>
        /// The numeric range that the value can be. Note that, the unit of this range
        /// will be forced to use the canonical unit of the variable, regardless of what
        /// you set
        /// </summary>
        public Range? AcceptedRange { get; set; }

        /// <summary>
        /// The numeric range that the value should be normally.
        /// If value inputted outside of this range, but still within <see cref="AcceptedRange"/>,
        /// it will still be accepted, but the client should be warned. Note that, the unit of this range
        /// will be forced to use the canonical unit of the variable, regardless of what
        /// </summary>
        public Range? NormalRange { get; set; }

        /// <summary>
        /// The categorical values that the value can be
        /// </summary>
        public List<string> AcceptedValues { get; set; } = [];

        public UpdateClinicalVariableCommand ToCommand(Guid id)
        {
            return new UpdateClinicalVariableCommand
            {
                Id = id,
                Code = Code,
                Name = Name,
                Description = Description,
                ValueType = ValueType,
                CanonicalUnit = CanonicalUnit,
                IsRequired = IsRequired,
                Category = Category,
                Prerequisite = Prerequisite,
                AcceptedRange = AcceptedRange,
                NormalRange = NormalRange,
                AcceptedValues = AcceptedValues,
            };
        }
    }
}
