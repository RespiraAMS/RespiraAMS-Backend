using Respira.Patient.Application.Features.Patients.GetPagedPatient;
using Respira.Patient.Application.Features.Patients.UpdatePatient;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Patient.API.Dtos
{
    public record GetPagedPatientRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string? Name { get; set; }
        public string? MedicalRecordCode { get; set; }
        public Gender? Gender { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }

        public GetPagedPatientQuery ToQuery()
        {
            return new GetPagedPatientQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size,
                },
                Filter = new PatientFilter
                {
                    Name = Name,
                    MedicalRecordCode = MedicalRecordCode,
                    Gender = Gender,
                    MinAge = MinAge,
                    MaxAge = MaxAge,
                }
            };
        }
    }

    public record UpdatePatientRequestDto
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string PhoneNumber { get; set; }
        public required DateOnly DateOfBirth { get; set; }
        public required Gender Gender { get; set; }
        public required decimal Height { get; set; }
        public required decimal Weight { get; set; }
        public required string MedicalRecordCode { get; set; }
        public required string HealthInsuranceCard { get; set; }
        public required string Address { get; set; }
        public required string City { get; set; }

        public UpdatePatientCommand ToCommand(Guid id)
        {
            return new UpdatePatientCommand
            {
                Id = id,
                FirstName = FirstName,
                LastName = LastName,
                PhoneNumber = PhoneNumber,
                DateOfBirth = DateOfBirth,
                Gender = Gender,
                Height = Height,
                Weight = Weight,
                MedicalRecordCode = MedicalRecordCode,
                HealthInsuranceCard = HealthInsuranceCard,
                Address = Address,
                City = City,
            };
        }
    }
}
