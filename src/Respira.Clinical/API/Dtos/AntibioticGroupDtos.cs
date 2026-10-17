using Respira.Clinical.Application.Features.AntibioticGroups.GetPagedAntibioticGroup;
using Respira.Clinical.Application.Features.AntibioticGroups.SearchAntibioticGroup;
using Respira.Clinical.Application.Features.AntibioticGroups.UpdateAntibioticGroup;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedAntibioticGroupRequestDto
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
        /// Antibiotic group parent ID
        /// </summary>
        public Guid? ParentId { get; set; }

        public GetPagedAntibioticGroupQuery ToQuery()
        {
            return new GetPagedAntibioticGroupQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size,
                },
                Filter = new AntibioticGroupFilter
                {
                    ParentId = ParentId
                }
            };
        }
    }

    public record SearchAntibioticGroupRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;

        public SearchAntibioticGroupQuery ToQuery()
        {
            return new SearchAntibioticGroupQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size
                },
                SearchTerm = SearchTerm
            };
        }
    }

    public record UpdateAntibioticGroupRequestDto
    {
        /// <summary>
        /// Antibiotic group name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Antibiotic group description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Antibiotic group parent ID
        /// </summary>
        public Guid? ParentId { get; set; }

        public UpdateAntibioticGroupCommand ToCommand(Guid id)
        {
            return new UpdateAntibioticGroupCommand
            {
                Id = id,
                Name = Name,
                Description = Description,
                ParentId = ParentId
            };
        }
    }
}
