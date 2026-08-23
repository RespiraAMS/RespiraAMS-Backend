using Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
    /// <summary>
    /// Doctor profile containing professional information and hierarchical relationships.
    /// </summary>
    public class Doctor : Base
    {
        /// <summary>Doctor first name</summary>
        public required string FirstName { get; set; }

        /// <summary>Doctor last name</summary>
        public required string LastName { get; set; }

        /// <summary>Academic degrees held by the doctor</summary>
        public required ICollection<DegreeType> Degrees { get; set; }

        /// <summary>Academic title</summary>
        public required AcademicTitleEnum AcademicTitle { get; set; }

        /// <summary>Hospital position</summary>
        public required PositionType Position { get; set; }

        /// <summary>Identifiers of patients managed by the doctor</summary>
        public ICollection<Guid>? Patients { get; set; }

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

        /// <summary>Doctors subordinate to this doctor (hierarchy)</summary>
        public ICollection<Doctor>? Subordinates { get; set; }
    }
}
