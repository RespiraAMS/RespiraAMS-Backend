using Domain.Enums;

namespace Application.Features.Treatments.CreateTreatment;

public class CreateTreatmentCommand : ICommand
{
    public required Guid PatientId { get; set; }
    public required Guid DoctorId { get; set; }
    public required List<MedicineRecord> MedicineRecords { get; set; }
    public required TreatmentType TreatmentType { get; set; }
    public required string Severity { get; set; }
    public required string TreatmentSite { get; set; }
    public List<InfectionProbabilityRecord> InfectionProbabilityRecords { get; set; } = [];
    public required string Pathogen { get; set; }
}

public class CreateTreatmentResult(Guid id)
{
    public Guid Id { get; set; } = id;
}
