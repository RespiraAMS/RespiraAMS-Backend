using System.Text.Json;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment
{
    public record ConfirmTreatmentQuery : IQuery
    {
        public required Guid DoctorId { get; set; }
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
    }

    /// <summary>
    /// The subset of the diagnosis payload the clinical service actually needs.
    /// Everything else is forwarded to the patient service untouched.
    /// <para>
    /// Deliberately minimal: deserializing the full <c>DiagnoseResult</c> would
    /// drag in the domain graph (see <see cref="ConfirmTreatmentQuery.Diagnosis"/>),
    /// and the adherence check only ever needs the medicine IDs of each protocol
    /// combination.
    /// </para>
    /// </summary>
    public record ConfirmDiagnosisView
    {
        /// <summary>
        /// The diagnosis being confirmed.
        /// </summary>
        public Guid DiagnosisId { get; set; }

        /// <summary>
        /// Recommended treatment protocols. Each protocol holds a list of
        /// medicine combinations, and each combination a list of medicines.
        /// </summary>
        public IEnumerable<ConfirmProtocolView> Protocols { get; set; } = [];
    }

    public record ConfirmProtocolView
    {
        public IEnumerable<IEnumerable<ConfirmMedicineView>> Medicines { get; set; } = [];
    }

    public record ConfirmMedicineView(Guid Id);
}
