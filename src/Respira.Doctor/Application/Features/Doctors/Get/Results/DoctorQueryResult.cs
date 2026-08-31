using Domain.Enums;
using Wolverine.Attributes;

namespace Application.Features.Doctors.Get.Results
{
    [MessageIdentity("doctor-result")]
    public record DoctorQueryResult
    {
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string Role { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public required string Status { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required ICollection<string> Degrees { get; set; }
        public required string AcademicTitle { get; set; }
        public required PositionType Position { get; set; }
        public ICollection<Guid>? Patients { get; set; }
        public required string Gender { get; set; }
        public required string CitizenIdentificationNumber { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public required string Address { get; set; }
        public Guid? MediaId { get; set; }
        public ICollection<Guid>? Subordinates { get; set; }
        public string? Url { get; set; }
    }
}
