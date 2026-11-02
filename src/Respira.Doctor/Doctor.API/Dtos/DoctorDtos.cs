using Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor;
using Respira.Doctor.Application.Features.Doctors.Queries.SearchDoctor;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Doctor.API.Dtos
{
    public record GetPagedDoctorRequestDto
    {
        /// <summary>Pagination parameter: page index.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Pagination parameter: page size.</summary>
        public int Size { get; set; } = 10;

        /// <summary>Kind of staff profile (Doctor or HeadDepartmentDoctor).</summary>
        public ProfileRole? Role { get; set; }

        /// <summary>Profile gender.</summary>
        public GenderType? Gender { get; set; }

        /// <summary>Academic title.</summary>
        public AcademicTitle? AcademicTitle { get; set; }

        /// <summary>Hospital position.</summary>
        public PositionType? Position { get; set; }

        public GetPagedDoctorQuery ToQuery()
        {
            return new GetPagedDoctorQuery
            {
                Param = new PaginationParam { Page = Page, Size = Size },
                Filter = new DoctorFilter
                {
                    Role = Role,
                    Gender = Gender,
                    AcademicTitle = AcademicTitle,
                    Position = Position,
                },
            };
        }
    }

    public record SearchDoctorRequestDto
    {
        /// <summary>Pagination parameter: page index.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Pagination parameter: page size.</summary>
        public int Size { get; set; } = 10;

        /// <summary>Free text matched against first name, last name and citizen id.</summary>
        public string SearchTerm { get; set; } = string.Empty;

        public SearchDoctorQuery ToQuery()
        {
            return new SearchDoctorQuery
            {
                Param = new PaginationParam { Page = Page, Size = Size },
                SearchTerm = SearchTerm,
            };
        }
    }
}
