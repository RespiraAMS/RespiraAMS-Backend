using Respira.Doctor.Domain.Enums;

namespace Respira.Doctor.Application.Features.Doctors.Common
{
    /// <summary>
    /// Shared shape for doctor/manager profiles returned by the list and search queries.
    /// The account and media fields are filled by <c>IDoctorProfileEnricher</c>.
    /// </summary>
    public abstract record DoctorProfileItem
    {
        /// <summary>Profile identifier.</summary>
        public required Guid Id { get; set; }

        /// <summary>Account this profile is linked to.</summary>
        public required Guid AccountId { get; set; }

        /// <summary>Kind of staff profile (Doctor or HeadDepartmentDoctor).</summary>
        public ProfileRole Role { get; set; }

        /// <summary>Profile first name.</summary>
        public required string FirstName { get; set; }

        /// <summary>Profile last name.</summary>
        public required string LastName { get; set; }

        /// <summary>Academic degrees held.</summary>
        public required ICollection<DegreeType> Degrees { get; set; }

        /// <summary>Academic title.</summary>
        public AcademicTitle AcademicTitle { get; set; }

        /// <summary>Hospital position.</summary>
        public PositionType Position { get; set; }

        /// <summary>Gender.</summary>
        public GenderType Gender { get; set; }

        /// <summary>Vietnam citizen identification number (CCCD/CMND).</summary>
        public required string CitizenIdentificationNumber { get; set; }

        /// <summary>Date of birth (if provided).</summary>
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>Residential address.</summary>
        public required string Address { get; set; }

        /// <summary>Avatar media identifier (if linked).</summary>
        public Guid? MediaId { get; set; }

        /// <summary>Profile creation timestamp.</summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>Account email resolved from the authentication service.</summary>
        public string? Email { get; set; }

        /// <summary>Account phone number resolved from the authentication service.</summary>
        public string? Phone { get; set; }

        /// <summary>Account role resolved from the authentication service.</summary>
        public string? AccountRole { get; set; }

        /// <summary>Account status resolved from the authentication service.</summary>
        public string? AccountStatus { get; set; }

        /// <summary>Avatar URL resolved from the media service.</summary>
        public string? AvatarUrl { get; set; }
    }
}
