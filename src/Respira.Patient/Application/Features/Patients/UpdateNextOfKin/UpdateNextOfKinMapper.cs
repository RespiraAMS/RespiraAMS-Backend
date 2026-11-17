using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Features.Patients.UpdateNextOfKin
{
    public class UpdateNextOfKinMapper : IUpdateMapper<NextOfKin, UpdateNextOfKinCommand>
    {
        public Result MapModel(NextOfKin model, UpdateNextOfKinCommand command)
        {
            model.FullName = command.FullName;
            model.PhoneNumber = command.PhoneNumber;
            model.Relationship = command.Relationship;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }
    }
}
