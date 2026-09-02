using Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Domain.Models;

/*
 * An antibiotic can have:
 * 1. Name
 * 2. Antibiotic group
 * 3. AWaRe category
 * 4. Route of administration
 * 5. Dosage
 * 6. Antibiotic spectra (the list of pathogen that this antibiotic can take effect). Since this is a technical
 * term, we will use the spectra instead of Pathogens for the property name
 *
 * For dosage, there are some rules:
 * 1. Dosage is grouped by route of administration
 * 2. Dosage can be:
 * 2.1. Standard dose
 * 2.2. Adjust dose (based on creatine clearance- CrCl)
 *
 * Because of how complex dosage can become, it will be extracted into a new class
 */

/// <summary>
/// Antibiotic class. Sometimes refer as medicine
/// </summary>
public class Antibiotic : Base
{
    /// <summary>
    /// Antibiotic name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Antibiotic group ID
    /// </summary>
    public required Guid AntibioticGroupId { get; set; }

    /// <summary>
    /// Antibiotic group
    /// </summary>
    public AntibioticGroup AntibioticGroup { get; set; } = null!;

    /// <summary>
    /// Antibiotic AWaRe category
    /// </summary>
    public required AwareClassification Classification { get; set; }

    /// <summary>
    /// List of dosage IDs
    /// </summary>
    public List<Guid> DosageIds { get; set; } = [];

    /// <summary>
    /// Antibiotic dosages
    /// </summary>
    public List<Dosage> Dosages { get; set; } = [];

    /// <summary>
    /// List of pathogen IDs (which is corresponding to <see cref="AntibioticSpectra"/>)
    /// </summary>
    public List<Guid> PathogenIds { get; set; } = [];

    /// <summary>
    /// Antibiotic spectra
    /// </summary>
    public List<Pathogen> AntibioticSpectra { get; set; } = [];

    /// <summary>
    /// Validate if antibiotic's dosage is valid according to business rules
    /// </summary>
    /// <param name="dosages">Antibiotic dosage</param>
    /// <returns>Result object of boolean</returns>
    public static Result<bool> IsAntibioticDosageValid(List<Dosage> dosages)
    {
        // Antibiotic dosage should adhere to these rules
        // 1. There must be at least 1 dosage regardless of route of administration
        // 2. For each route of administration (if exists), there must be 1 and only 1
        // standard dose (CrCl == null)
        // 3. For each route, CrCl must not overlapped with any other CrCl

        // Check rule 1
        if (!dosages.Any())
        {
            return Result<bool>.Failure(new Error(Status.BusinessRuleViolation, "Dosage list is empty"));
        }

        foreach (var route in dosages.Select(d => d.RouteOfAdministration).Distinct().ToList())
        {
            var dosagePerRoute = dosages
                .Where(d => d.RouteOfAdministration == route)
                .ToList();

            // Check rule 2
            if (dosagePerRoute.Count(d => d.Crcl == null) != 1)
            {
                var msg = $"Route {route} has more than 1 standard dose";
                return Result<bool>.Failure(new Error(Status.BusinessRuleViolation, msg));
            }

            // Check rule 3
            for (var i = 0; i < dosagePerRoute.Count - 1; i++)
            {
                // Because we have ensure that each route can only have 1 dosage with CrCl is null,
                // we can simply ignore null case in this check
                if (dosagePerRoute[i].Crcl is null) continue;
                for (var j = i + 1; j < dosagePerRoute.Count; j++)
                {
                    if (dosagePerRoute[j].Crcl is null) continue;
                    if (dosagePerRoute[i].Crcl!.IsRangeOverlapped(dosagePerRoute[j].Crcl))
                    {
                        var msg = $"Route {route} has overlapped CrCl ranges";
                        return Result<bool>.Failure(new Error(Status.BusinessRuleViolation, msg, new
                        {
                            Range1 = dosagePerRoute[i].Crcl!,
                            Range2 = dosagePerRoute[j].Crcl!
                        }));
                    }
                }
            }
        }

        return Result<bool>.Success(Status.Success, true);
    }
}
