using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.AddNextOfKin
{
    public class AddNextOfKinMapper : ICreateMapper<AddNextOfKinCommand, NextOfKin>
    {
        public Result<NextOfKin> ToModel(AddNextOfKinCommand command)
        {
            return Result<NextOfKin>.Success(ApplicationStatus.Success, new NextOfKin
            {
                PatientId = command.PatientId,
                FullName = command.FullName,
                PhoneNumber = command.PhoneNumber,
                Relationship = command.Relationship
            });
        }
    }
}
