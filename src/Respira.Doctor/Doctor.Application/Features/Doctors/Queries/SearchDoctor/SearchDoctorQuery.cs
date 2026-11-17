using Respira.Doctor.Application.Features.Doctors.Common;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Doctor.Application.Features.Doctors.Queries.SearchDoctor
{
    public record SearchDoctorQuery : IQuery
    {
        /// <summary>Pagination parameter.</summary>
        public required PaginationParam Param { get; set; }

        /// <summary>Free text used to match the doctor first name, last name or citizen id.</summary>
        public required string SearchTerm { get; init; }
    }

    public record SearchDoctorItem : DoctorProfileItem
    {
        /// <summary>Search rank (most relevant to search term).</summary>
        public float Rank { get; set; }
    }
}
