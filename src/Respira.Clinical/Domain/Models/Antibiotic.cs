using Domain.Enums;

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
 * 2.2. Adjust dose (based on glomerular filtration rate - GFR)
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
    public required AwareCategory Category { get; set; }

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
}