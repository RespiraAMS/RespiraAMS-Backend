using Respira.Doctor.Domain.Enums;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetById.Result
{
    /// <summary>Projection of a doctor (or manager) profile returned by the get-by-id query.</summary>
    public record DoctorResult
    {
        /// <summary>Profile identifier</summary>
        public Guid Id { get; set; }

        /// <summary>Account this profile is linked to</summary>
        public Guid AccountId { get; set; }

        /// <summary>Kind of staff profile (Doctor or Manager)</summary>
        public ProfileRole Role { get; set; }

        /// <summary>Profile first name</summary>
        public required string FirstName { get; set; }

        /// <summary>Profile last name</summary>
        public required string LastName { get; set; }

        /// <summary>Academic degrees held</summary>
        public required List<DegreeType> Degrees { get; set; }

        /// <summary>Academic title</summary>
        public AcademicTitle AcademicTitle { get; set; }

        /// <summary>Hospital position</summary>
        public PositionType Position { get; set; }

        /// <summary>Gender</summary>
        public GenderType Gender { get; set; }

        /// <summary>Vietnam citizen identification number (CCCD/CMND)</summary>
        public required string CitizenIdentificationNumber { get; set; }

        /// <summary>Date of birth (if provided)</summary>
        public DateTimeOffset? DateOfBirth { get; set; }

        /// <summary>Residential address</summary>
        public required string Address { get; set; }

        /// <summary>Avatar media identifier (if linked)</summary>
        public Guid? MediaId { get; set; }

        /// <summary>Profile creation timestamp</summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
}
