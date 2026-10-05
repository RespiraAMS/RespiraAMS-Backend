using Respira.Clinical.Application.Features.Pathogens.GetPagedPathogen;
using Respira.Clinical.Application.Features.Pathogens.UpdatePathogen;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Clinical.API.Dtos
{
    public record GetPagedPathogenRequestDto
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
        /// Pathogen name
        /// </summary>
        public string? Name { get; set; }

        public GetPagedPathogenQuery ToQuery()
        {
            return new GetPagedPathogenQuery()
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size,
                },
                Filter = new PathogenFilter()
                {
                    Name = Name,
                }
            };
        }
    }

    public record UpdatePathogenRequestDto
    {
        /// <summary>
        /// Pathogen name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Pathogen description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Boolean flag to indicate if the pathogen is atypical.
        /// </summary>
        public bool IsAtypical { get; set; }

        public UpdatePathogenCommand ToCommand(Guid id)
        {
            return new UpdatePathogenCommand
            {
                Id = id,
                Name = Name,
                Description = Description,
                IsAtypical = IsAtypical
            };

        }
    }
}
