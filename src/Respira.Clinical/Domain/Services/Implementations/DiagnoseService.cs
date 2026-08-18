using Domain.Models;
using Domain.Services.Dtos;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Exceptions;

namespace Domain.Services.Implementations;

public class DiagnoseService(ILogger<DiagnoseService> logger)
{
    /// <summary>
    /// Calculate creatine clearance using Cockcroft-Gault equation
    /// </summary>
    /// <param name="age">Patient age (in years)</param>
    /// <param name="weight">Patient weight (in kg)</param>
    /// <param name="height">Patient's height (in meter)</param>
    /// <param name="scr">Serum creatine (mg/dL)</param>
    /// <param name="isMale">Boolean flag: is the patient male (default) or female</param>
    /// <returns>Creatine clearance (ml/minute)</returns>
    protected virtual decimal CrCl(int age, decimal weight, decimal height, decimal scr, bool isMale = true)
    {
        if (height <= 0 || weight <= 0 || scr <= 0)
        {
            throw new BadRequestException("Weight, height and serum creatine must be positive value");
        }

        // Calculate patient's BMI
        var bmi = weight / (height * height);

        // If patient is obese, use Salazar - Corcoran
        if (bmi >= 30)
        {
            return isMale ?
                (137 - age) * ((0.285m * weight) + (12.1m * height * height)) / (51 * scr) :
                (146 - age) * ((0.287m * weight) + (9.74m * height * height)) / (60 * scr);
        }

        // If not, use Cockcroft-Gault
        var crcl = (140 - age) * weight / (72 * scr);
        return isMale ? crcl : crcl * 0.85m;
    }

    /// <summary>
    /// Assess if this patient need treatment in Intensive Care Unit (ICU). If any option didn't exist in criteria,
    /// that option will be ignored and continue to the next option
    /// </summary>
    /// <param name="criteria">The list of all criteria for assessment</param>
    /// <param name="scoreThreshold">The minimum score threshold to consider needing ICU</param>
    /// <param name="options">
    /// The list of ICU criteria IDs that patient had. For example, if ICU criteria has 2 criteria A and B,
    /// and patient condition match A condition, then options will contain A's ID (IcuHospitalizeCriterion.CriterionId,
    /// not IcuHospitalizeCriteria.Id). See <see cref="IcuHospitalizeCriterion"/> for more detail
    /// </param>
    /// <returns>A boolean flag, true if patient need ICU</returns>
    protected virtual bool NeedIcu(List<IcuHospitalizeCriterion> criteria, int scoreThreshold, List<Guid> options)
    {
        var score = 0;

        foreach (var option in options)
        {
            var criterion = criteria.Find(c => c.CriterionId == option);
            if (criterion is null)
            {
                logger.LogWarning("ICU hospitalize option does not exists in criteria: {ID}", option);
                continue;
            }

            score += criterion.Score;
        }

        return score >= scoreThreshold;
    }

    /// <summary>
    /// Calculate the infection probability based on a list of criteria.
    /// </summary>
    /// <param name="factors">All the resistance risk factors for assessment</param>
    /// <param name="options">
    /// The list of resistance risk factor IDs that patient had. For example, if resistance risk factors have A and B,
    /// and patient condition match A condition, then options will contain A's ID (ResistanceRiskFactor.CriterionId,
    /// not ResistanceRiskFactor.Id). See <see cref="ResistanceRiskFactor"/> for more detail.
    /// </param>
    /// <returns>A list of <see cref="InfectionProbability"/> record</returns>
    /// <exception cref="BadRequestException">Throw if any option didn't exist in ResistanceRiskFactor list</exception>
    protected virtual IEnumerable<InfectionProbability> InfectionProbability(List<ResistanceRiskFactor> factors,
        List<Guid> options)
    {
        options = [.. options.Distinct()];
        var scores = new Dictionary<Guid, int>();
        foreach (var option in options)
        {
            var factor = factors.FirstOrDefault(x => x.CriterionId == option);
            if (factor is null)
            {
                logger.LogWarning("Resistance risk factor not found: {CriterionId}", option);
                throw new BadRequestException("Resistance risk factor not found");
            }

            if (scores.TryGetValue(factor.PathogenId, out _))
            {
                scores[factor.PathogenId]++;
            }
            else
            {
                scores.Add(factor.PathogenId, 1);
            }
        }

        var probabilities = new List<InfectionProbability>();
        foreach (var factor in factors.GroupBy(x => x.PathogenId))
        {
            var key = factor.Key;
            if (!scores.TryGetValue(key, out _)) continue;
            var value = (decimal)scores[key] / factor.Count();
            probabilities.Add(new InfectionProbability(factor.First().Pathogen, value));
        }

        return probabilities;
    }

    protected static decimal DataNormalization(decimal value, decimal min, decimal max)
    {
        return max == min ? 0 : (value - min) / (max - min);
    }

    protected virtual List<Antibiotic> GetAdjustedDosage(List<Antibiotic> antibiotics, decimal crcl)
    {
        foreach (var antibiotic in antibiotics)
        {
            // Get standard dose and adjusted dose
            // Standard dose can have many (one per route of administration),
            // so we get it as a list. For validation (same route but 2 standard dose, empty list,...),
            // we'll just assume that the data passed to this method is valid
            var standard = antibiotic.Dosages.Where(x => x.Crcl == null).ToList();
            var adjusted = antibiotic.Dosages
                .Where(x => x.Crcl?.IsInRange(crcl) == true)
                .ToList();

            // If no adjusted dose found, use standard dose
            antibiotic.Dosages = adjusted.Count == 0 ? standard : adjusted;
        }

        return antibiotics;
    }

    protected virtual List<Antibiotic> GetRecommendedMedicines(List<Antibiotic> antibiotics)
    {
        /*
         * Rule of picking antibiotic for treatment: for each group, can only pick
         * one medicine
         */

        var recommended = new List<Antibiotic>();

        // Group medicine by antibiotic group
        foreach (var groupedAntibiotics in antibiotics.GroupBy(x => x.AntibioticGroup.Id))
        {
            // Sort by AWaRe classification (ascending), picked the first one
            // Since we group by antibiotic group from the list of antibiotics, the
            // grouped list must have at least one antibiotic
            recommended.Add(groupedAntibiotics.OrderBy(x => x.Classification).First());
        }

        return recommended;
    }
}
