using System.Text.Json;
using Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment;

namespace Respira.Clinical.API.Dtos
{
    public record ConfirmTreatmentRequestDto
    {
        public required Guid PatientId { get; set; }

        /// <summary>
        /// The diagnosis result returned by the diagnose endpoint, echoed back
        /// by the client verbatim.
        /// <para>
        /// This is intentionally kept as raw JSON. <c>DiagnoseResult</c> contains
        /// domain models (notably <c>Evidence</c>, which has several constructors,
        /// and the abstract <c>ClinicalVariable</c>) that are only meant to be
        /// produced, not re-hydrated. Binding them would fail, and binding only
        /// the DTO shaped parts would silently drop polymorphic data such as
        /// <c>ScoreMetricsSeverityDiagnosis.Score</c>.
        /// </para>
        /// <para>
        /// Keeping the payload raw also means the exact snapshot the doctor saw is
        /// what gets forwarded to the patient service.
        /// </para>
        /// </summary>
        public JsonElement Diagnosis { get; set; }

        public required List<Guid> Medicines { get; set; }

        public string? ReasonForMedicineChoice { get; set; }

        public ConfirmTreatmentQuery ToQuery(Guid doctorId)
        {
            return new ConfirmTreatmentQuery
            {
                DoctorId = doctorId,
                PatientId = PatientId,
                Diagnosis = Diagnosis,
                Medicines = Medicines,
                ReasonForMedicineChoice = ReasonForMedicineChoice
            };
        }
    }
}
