using Respira.Doctor.Application.Features.Doctors.Common;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor
{
    public record DoctorFilter
    {
        /// <summary>Kind of staff profile (Doctor or HeadDepartmentDoctor).</summary>
        public ProfileRole? Role { get; set; }

        /// <summary>Profile gender.</summary>
        public GenderType? Gender { get; set; }

        /// <summary>Academic title.</summary>
        public AcademicTitleEnum? AcademicTitle { get; set; }

        /// <summary>Hospital position.</summary>
        public PositionType? Position { get; set; }
    }

    public record GetPagedDoctorQuery : IQuery
    {
        /// <summary>Pagination parameter.</summary>
        public required PaginationParam Param { get; set; }

        /// <summary>Doctor filter.</summary>
        public DoctorFilter? Filter { get; set; } = new();
    }

    public record PagedDoctorItem : DoctorProfileItem { }
}
