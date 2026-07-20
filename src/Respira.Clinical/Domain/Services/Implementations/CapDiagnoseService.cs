using Domain.Enums;
using Domain.Models;
using Domain.Services.Contracts;
using Domain.Services.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Respira.ServiceDefaults.Exceptions;

namespace Domain.Services.Implementations;

/// <summary>
/// Diagnose service for Community-Acquired Pneumonia (CAP)
/// </summary>
public sealed class CapDiagnoseService(ILogger<CapDiagnoseService> logger, IOptions<TuningCoefficient> coefficient)
    : DiagnoseService(logger), IDiagnoseService
{
    /// <summary>
    /// Calculate CURB65 score. Although this method is called Curb65, it can also be used for CRB65 by providing
    /// urea with null value
    /// </summary>
    /// <param name="confusion">Boolean flag: does the patient has confusion</param>
    /// <param name="urea">urea in blood (mmol/L)</param>
    /// <param name="respiratory">Respiratory rate per minute</param>
    /// <param name="systolic">Systolic blood pressure (mmHg)</param>
    /// <param name="diastolic">Diastolic blood pressure (mmHg)</param>
    /// <param name="age">Patient age</param>
    /// <returns>A tuple of (<see cref="Severity"/>, <see cref="TreatmentSite"/>)</returns>
    /// <exception cref="UnexpectedException">
    /// Throw exception if the score calculated get an unexpected value
    /// </exception>
    private (Severity, TreatmentSite) Curb65(
        bool confusion,
        decimal? urea,
        int respiratory,
        decimal systolic,
        decimal diastolic,
        int age)
    {
        var score = 0;

        // Calculate CURB-65 score
        if (confusion) score++;
        if (urea > 7) score++;
        if (respiratory >= 30) score++;
        if (systolic < 90 || diastolic < 60) score++;
        if (age >= 65) score++;

        // Return result using CURB-65
        if (urea is null)
        {
            return score switch
            {
                0 => (Severity.Mild, TreatmentSite.Outpatient),
                1 or 2 => (Severity.Moderate, TreatmentSite.Inpatient),
                3 or 4 => (Severity.Severe, TreatmentSite.IntensiveCareUnit),
                _ => throw new UnexpectedException($"CRB65 score get an unexpected value: {score}")
            };
        }

        // Return result using CRB-65
        return score switch
        {
            >= 0 and <= 1 => (Severity.Mild, TreatmentSite.Outpatient),
            2 => (Severity.Moderate, TreatmentSite.Inpatient),
            >= 3 and <= 5 => (Severity.Severe, TreatmentSite.IntensiveCareUnit),
            _ => throw new UnexpectedException($"CURB65 score get an unexpected value: {score}")
        };
    }

    public DiagnoseResult Diagnose(Disease disease, ClinicalPicture clinicalPicture)
    {
        // Calculate patient age
        var age = DateTimeOffset.UtcNow.Year - clinicalPicture.DateOfBirth.Year;

        // Calculate patient severity and treatment site using CURB-65
        var (severity, treatmentSite) = Curb65(
            clinicalPicture.Confusion,
            clinicalPicture.Urea,
            clinicalPicture.Respiratory,
            clinicalPicture.SystolicBloodPressure,
            clinicalPicture.DiastolicBloodPressure,
            age);

        // Calculate whether patient need ICU using AST metrics
        if (treatmentSite != TreatmentSite.IntensiveCareUnit &&
            NeedIcu(disease.IcuHospitalizeCriteria, disease.IcuScoreThreshold, clinicalPicture.IcuHospitalizeCriteria))
        {
            treatmentSite = TreatmentSite.IntensiveCareUnit;
        }

        // Calculate infection probabilities
        var probabilities = InfectionProbability(disease.ResistanceRiskFactors, clinicalPicture.ResistanceRiskFactors);

        // Filter protocols by severity and treatment site. In some rare cases, the patient may have a combination
        // of severity and treatment site that the current archive didn't have a corresponding protocol, so we
        // use OR, instead of AND, to make sure that there is at least 1 recommendation result
        var protocols = disease.EmpiricTreatmentProtocols
            .Where(p => p.Severity == severity || p.TreatmentSite == treatmentSite)
            .ToList();

        // Sort protocol with factors and coefficients
        decimal maxVersion = protocols.Max(x => x.Version);
        var minVersion = protocols.Min(x => x.Version);
        var minDay = protocols.Min(x => x.IssueDate).DayNumber;
        var maxDay = protocols.Max(x => x.IssueDate).DayNumber;
        var sorted = protocols.OrderByDescending(p =>
            {
                // Calculate severity score
                var score = p.Severity == severity ? coefficient.Value.SeverityMatchWeight : 0;

                // Calculate treatment site score
                score += p.TreatmentSite == treatmentSite ? coefficient.Value.TreatmentSiteMatchWeight : 0;

                // Calculate special infection score
                var probability = probabilities
                    .Where(x => x.Pathogen.Id == p.SpecialInfectionId)
                    .Select(x => x.Probability)
                    .FirstOrDefault(0);
                score += probability * coefficient.Value.SpecialInfectionWeight;

                // Calculate other criteria score
                decimal matched = p.OtherCriteria.Count(c => clinicalPicture.OtherCriteria.Contains(c.Id));
                var total = p.OtherCriteria.Count;
                score += total == 0 ? 0 : matched / total * coefficient.Value.CriteriaMatchWeight;

                // Calculate version score
                score += DataNormalization(p.Version, minVersion, maxVersion) * coefficient.Value.VersionWeight;

                // Calculate issue date score
                score += DataNormalization(p.IssueDate.DayNumber, minDay, maxDay) * coefficient.Value.IssueDateWeight;

                return score;
            })
            .ToList();


        // Calculate GFR and filter out medicine dosage
        var gfr = CrCl(age, clinicalPicture.Weight, clinicalPicture.SerumCreatine, clinicalPicture.IsMale);
        foreach (var protocol in protocols)
        {
            protocol.Medicines.ForEach(m =>
            {
                m.Dosages = m.Dosages.Where(d => d.GlomerularFiltrationRate.IsInRange(gfr)).ToList();
            });
        }

        // Construct diagnose result
        var result = new DiagnoseResult()
        {
            Severity = severity,
            TreatmentSite = treatmentSite,
            InfectionProbabilities = probabilities.ToList(),
            Recommendations = sorted,
        };

        return result;
    }
}