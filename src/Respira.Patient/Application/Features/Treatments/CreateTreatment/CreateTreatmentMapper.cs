using Domain.Enums;

namespace Application.Features.Treatments.CreateTreatment;

public class CreateTreatmentMapper : ICreateMapper<Treatment, CreateTreatmentCommand>
{
    public Treatment ToModel(CreateTreatmentCommand command)
    {
        return command.TreatmentType == TreatmentType.TargetedTherapy ?
            new TargetedTreatment
            {
                PatientId = command.PatientId,
                DoctorId = command.DoctorId,
                Status = PatientTreatmentStatus.FavorableResponse,
                TargetedDiagnosisRecord = (TargetedDiagnosisRecord)command.DiagnosisRecord ??
                    throw new ArgumentException("Failed to map targeted diagnosis record for create treatment: diagnosis record is not targted"),
            } :
            new EmpiricalTreatment
            {
                PatientId = command.PatientId,
                DoctorId = command.DoctorId,
                Status = PatientTreatmentStatus.FavorableResponse,
                EmpiricalDiagnosisRecord = (EmpiricalDiagnosisRecord)command.DiagnosisRecord ??
                    throw new ArgumentException("Failed to map empirical diagnosis record for create treatment: diagnosis record is not empirical"),
            };
    }
}
