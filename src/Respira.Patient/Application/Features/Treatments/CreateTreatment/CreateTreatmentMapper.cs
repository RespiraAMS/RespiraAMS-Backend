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
                Crcl = command.Crcl,
                MedicineRecords = command.MedicineRecords,
                Status = PatientTreatmentStatus.FavorableResponse,
                Pathogen = command.Pathogen ??
                    throw new UnexpectedException("Failed to map targeted therapy for create treatment: pathogen unexpectedly null"),
            } :
            new EmpiricalTreatment
            {
                PatientId = command.PatientId,
                DoctorId = command.DoctorId,
                Crcl = command.Crcl,
                MedicineRecords = command.MedicineRecords,
                Status = PatientTreatmentStatus.FavorableResponse,
                Severity = command.Severity ??
                    throw new UnexpectedException("Failed to map empirical therapy for create treatment: severity unexpectedly null"),
                TreatmentSite = command.TreatmentSite ??
                    throw new UnexpectedException("Failed to map empirical therapy for create treatment: treatment site unexpectedly null"),
                InfectionProbabilityRecords = command.InfectionProbabilityRecords ??
                    throw new UnexpectedException("Failed to map empirical therapy for create treatment: infection probability records unexpectedly null"),
            };
    }
}
