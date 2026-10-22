using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Saga.Application.Constracts.Messages.Doctor.Create
{
    public record CreateDoctorProfileCommand : ICommand
    {
        public Guid SagaId { get; set; }
        public Guid AccountId { get; set; }
        public ProfileRole Role { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required List<DegreeType> Degrees { get; set; }
        public AcademicTitleEnum AcademicTitle { get; set; }
        public PositionType Position { get; set; }
        public GenderType Gender { get; set; }
        public required string CitizenIdentificationNumber { get; set; }
        public DateTimeOffset? DateOfBirth { get; set; }
        public required string Address { get; set; }
    }
}
