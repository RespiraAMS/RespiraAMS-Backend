using Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetSubordinates
{
    public record SubordinateDoctorFilter
    {
        /// <summary>Profile gender.</summary>
        public GenderType? Gender { get; set; }

        /// <summary>Academic title.</summary>
        public AcademicTitle? AcademicTitle { get; set; }

        /// <summary>Hospital position.</summary>
        public PositionType? Position { get; set; }

        /// <summary>Free text matched against first name, last name and citizen id.</summary>
        public string? SearchTerm { get; set; }
    }

    public record GetSubordinateDoctorsQuery : IQuery
    {
        /// <summary>The account identifier of the head of department doctor requesting the list.</summary>
        public required Guid HeadDoctorAccountId { get; set; }

        /// <summary>Pagination parameter.</summary>
        public required PaginationParam Param { get; set; }

        /// <summary>Subordinate doctor filter.</summary>
        public SubordinateDoctorFilter? Filter { get; set; } = new();
    }
}
