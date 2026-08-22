using Domain.Enums;
using Respira.ServiceDefaults.Constracts.CQRS;

namespace Application.Features.Doctors.Create.Commands
{
    public record CreateDoctorCommand : ICommand
    {
        public required Guid SagaId { get; set; }
        public required Guid DoctorId { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required List<DegreeType> Degrees { get; set; }
        public required AcademicTitleEnum AcademicTitle { get; set; }
        public required PositionType Position { get; set; }
        public GenderType Gender { get; set; }
        public required string CitizenIdentificationNumber { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public required string Address { get; set; }
        public Guid? MediaId { get; set; }
        public Guid? DoctorCreatorId { get; set; }
    }
}
