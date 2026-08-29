namespace Domain.Models;

/// <summary>
/// An immutable record of medicine used for treatment
/// </summary>
/// <param name="Id">Antibiotic ID</param>
/// <param name="Name">Antibiotic name</param>
/// <param name="Classification">Antibiotic classification (WHO's AWaRe classification)</param>
/// <param name="RouteOfAdministration">Antibiotic route of administration</param>
/// <param name="Dose">
/// Actual dosage that was diagnosed for this patient, based on patient's GFR
/// </param>
public record MedicineRecord(Guid Id, string Name, string Classification, string RouteOfAdministration, string Dose);
