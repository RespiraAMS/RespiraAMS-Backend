using Application.Features.Treatments.CreateTreatment;
using Domain.Enums;
using Domain.Models;
using Respira.ServiceDefaults.Exceptions;

namespace Respira.Patient.API.Dtos
{
    public class CreateTreatmentRequestDto
    {
        public required DiagnosisRecordDto DiagnosisRecord { get; set; }

        /// <summary>
        /// Treatment type
        /// </summary>
        public required TreatmentType TreatmentType { get; set; }

        public CreateTreatmentCommand ToCommand(Guid patientId, Guid doctorId)
        {
            return new CreateTreatmentCommand
            {
                PatientId = patientId,
                DoctorId = doctorId,
                DiagnosisRecord = DiagnosisRecord.ToDiagnosisRecord(TreatmentType),
                TreatmentType = TreatmentType
            };
        }
    }

    public class DiagnosisRecordDto
    {
        public required decimal Crcl { get; set; }
        public required List<MedicineRecord> SystemRecommendedMedicines { get; set; }
        public required List<MedicineRecord> DoctorChosenMedicines { get; set; }
        public string? ReasonForDifferentChoice { get; set; }
        public string? Severity { get; set; }
        public string? TreatmentSite { get; set; }
        public List<InfectionProbabilityRecord>? InfectionProbabilityRecords { get; set; }
        public Domain.Models.PathogenRecord? Pathogen { get; set; }

        public DiagnosisRecord ToDiagnosisRecord(TreatmentType type)
        {
            if (type == TreatmentType.TargetedTherapy)
            {
                return new TargetedDiagnosisRecord
                {
                    Crcl = Crcl,
                    DoctorChosenMedicines = DoctorChosenMedicines,
                    ReasonForDifferentChoice = ReasonForDifferentChoice,
                    SystemRecommendedMedicines = SystemRecommendedMedicines,
                    Pathogen = Pathogen ?? throw new BadRequestException("Pathogen is required for targeted diagnosis record")
                };
            }

            return new EmpiricalDiagnosisRecord
            {
                Crcl = Crcl,
                SystemRecommendedMedicines = SystemRecommendedMedicines,
                DoctorChosenMedicines = DoctorChosenMedicines,
                ReasonForDifferentChoice = ReasonForDifferentChoice,
                InfectionProbabilityRecords = InfectionProbabilityRecords ?? throw new BadRequestException("Infection probability records are required for empirical diagnosis record"),
                Severity = Severity ?? throw new BadRequestException("Severity is required for empirical diagnosis record"),
                TreatmentSite = TreatmentSite ?? throw new BadRequestException("Treatment site is required for empirical diagnosis record")
            };
        }
    }
}
