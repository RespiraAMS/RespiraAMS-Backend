using Domain.Enums;
using Respira.ServiceDefaults.Models;

namespace Domain.Entities
{
    /// <summary>
    /// Doctor profile containing professional information and hierarchical relationships.
    /// </summary>
    public class Doctor : Base
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required ICollection<DegreeType> Degrees { get; set; }
        public required AcademicTitleEnum AcademicTitle { get; set; }
        public required PositionType Position { get; set; }
        public ICollection<Guid>? Patients { get; set; }
        public GenderType Gender { get; set; }
        public required string CitizenIdentificationNumber { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public required string Address { get; set; }
        public Guid? MediaId { get; set; }
        public ICollection<Doctor>? Subordinates { get; set; }
    }
}
