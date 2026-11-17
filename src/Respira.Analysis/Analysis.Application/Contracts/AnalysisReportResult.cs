namespace Respira.Analysis.Application.Contracts
{
    public sealed record AnalysisReportResult(
        Guid AnalysisId,
        Guid? DoctorId,
        DateTimeOffset Start,
        DateTimeOffset End,
        int TotalTreatmentPlans,
        int TotalPatients,
        int TotalAdherentPlans,
        int TotalNonAdherentPlans,
        int TotalPendingReviewPlans,
        int TotalDeceasedPatients,
        int TotalRecoveredPatients,
        int TotalOtherOutcomePatients,
        IReadOnlyCollection<DoctorMedicineAnalysisSummary> Doctors
    );

    public sealed record DoctorMedicineAnalysisSummary(
        Guid DoctorId,
        int TotalPatients,
        int TotalTreatmentPlans,
        int TotalAdherentPlans,
        int TotalNonAdherentPlans,
        int TotalPendingReviewPlans,
        IReadOnlyCollection<MedicineAnalysisSummary> Medicines
    );

    public sealed record MedicineAnalysisSummary(
        Guid MedicineId,
        string Name,
        string Classification,
        int TotalTreatmentPlans,
        int TotalAdherentPlans,
        int TotalNonAdherentPlans,
        int TotalPendingReviewPlans
    );
}
