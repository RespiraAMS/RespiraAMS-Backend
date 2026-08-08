namespace Application.Features.Patients.DischargePatient;

public class DischargePatientMapper : IUpdateMapper<Patient, DischargePatientCommand>
{
    public void MapModel(Patient model, DischargePatientCommand command)
    {
        model.Discharge = DateTimeOffset.UtcNow;
        model.Status = command.Status;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}