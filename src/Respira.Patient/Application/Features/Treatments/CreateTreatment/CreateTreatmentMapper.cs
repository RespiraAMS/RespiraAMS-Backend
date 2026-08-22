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
                MedicineRecords = command.MedicineRecords,
                Status = PatientTreatmentStatus.FavorableResponse,
                Pathogen = command.Pathogen
            } :
            new EmpiricalTreatment
            {
                PatientId = command.PatientId,
                DoctorId = command.DoctorId,
                MedicineRecords = command.MedicineRecords,
                Status = PatientTreatmentStatus.FavorableResponse,
                Severity = command.Severity,
                TreatmentSite = command.TreatmentSite,
                InfectionProbabilityRecords = command.InfectionProbabilityRecords
            };
    }
}
