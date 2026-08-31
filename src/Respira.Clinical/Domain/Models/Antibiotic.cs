using Domain.Enums;
using Domain.Exceptions;

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
    /// Validate if antibiotic dosage is valid
    /// </summary>
    /// <exception cref="DosageEmptyException">Throw if antibiotic dosage is empty</exception>
    /// <exception cref="StandardDoseInvalidException">
    /// Throw if violate rule "standard dose for route of administration is 1"
    /// </exception>
    /// <exception cref="OverlappedCrclException">
    /// Throw if violate rule "CrCl range is overlapped"
    /// </exception>
    public static void IsAntibioticDosageValid(List<Dosage> dosages)
    {
        // Antibiotic dosage should adhere to these rules
        // 1. There must be at least 1 dosage regardless of route of administration
        // 2. For each route of administration (if exists), there must be 1 and only 1
        // standard dose (CrCl == null)
        // 3. For each route, CrCl must not overlapped with any other CrCl

        // Check rule 1
        if (!dosages.Any())
        {
            throw new DosageEmptyException();
        }

        foreach (var route in dosages.Select(d => d.RouteOfAdministration).Distinct().ToList())
        {
            var dosagePerRoute = dosages
                .Where(d => d.RouteOfAdministration == route)
                .ToList();

            // Check rule 2
            if (dosagePerRoute.Count(d => d.Crcl == null) != 1)
            {
                throw new StandardDoseInvalidException(route);
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
                        throw new OverlappedCrclException(route, dosagePerRoute[i].Crcl!, dosagePerRoute[j].Crcl!);
                    }
                }
            }
        }
    }
}
