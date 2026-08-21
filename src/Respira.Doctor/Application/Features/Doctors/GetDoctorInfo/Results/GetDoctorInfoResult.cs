using Domain.Enums;

namespace Application.Features.Doctors.GetDoctorInfo.Results
{
    /// <summary>
    /// Doctor profile information returned by <see cref="Queries.GetDoctorInfoQuery"/>.
    /// </summary>
    public class GetDoctorInfoResult
    {
        /// <summary>Doctor identifier</summary>
        public Guid Id { get; set; }

        /// <summary>First name</summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Last name</summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>Academic degrees held by the doctor</summary>
        public List<DegreeType> Degrees { get; set; } = [];

        /// <summary>Academic title</summary>
        public AcademicTitleEnum AcademicTitle { get; set; }

        /// <summary>Position / role in the hospital</summary>
        public PositionType Position { get; set; }

        /// <summary>Gender</summary>
        public GenderType Gender { get; set; }

        /// <summary>Date of birth (if provided)</summary>
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>Address</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Reference to the doctor's avatar in the media service</summary>
        public Guid? MediaId { get; set; }
    }
}
