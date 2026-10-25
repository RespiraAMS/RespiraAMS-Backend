using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class Antibiotic : FullTextSearch
    {
        public required string Name { get; set; }
        public required Guid AntibioticGroupId { get; set; }
        public AntibioticGroup AntibioticGroup { get; set; } = null!;
        public required AwareClassification Classification { get; set; }
        public ICollection<Dosage> Dosages { get; set; } = [];

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
            // 3. For each route, CrCl must not overlap with any other CrCl

            // Check rule 1
            if (dosages.Count == 0)
            {
                return Result<bool>.Failure(new ApplicationError(ApplicationStatus.BusinessRuleViolation, "Dosage list is empty"));
            }

            foreach (var route in dosages.Select(d => d.RouteOfAdministration).Distinct().ToList())
            {
                var dosagePerRoute = dosages
                    .Where(d => d.RouteOfAdministration == route)
                    .ToList();

                // Check rule 2
                if (dosagePerRoute.Count(d => d.IsStandardDose) != 1)
                {
                    var msg = $"Route {route} has more than 1 standard dose";
                    return Result<bool>.Failure(new ApplicationError(ApplicationStatus.BusinessRuleViolation, msg));
                }

                // Check rule 3
                for (var i = 0; i < dosagePerRoute.Count - 1; i++)
                {
                    // Because we have ensured that each route can only have 1 dosage with CrCl is null,
                    // we can simply ignore null case in this check
                    // NOTE: in this case, the check should be a direct comparison of CrCl != null,
                    // instead of using the self infer IsStandardDose for clarity, even if the 2 is
                    // equal
                    if (dosagePerRoute[i].Crcl is null) continue;
                    for (var j = i + 1; j < dosagePerRoute.Count; j++)
                    {
                        if (dosagePerRoute[j].Crcl is null) continue;
                        if (dosagePerRoute[i].Crcl!.IsRangeOverlapped(dosagePerRoute[j].Crcl))
                        {
                            var msg = $"Route {route} has overlapped CrCl ranges";
                            return Result<bool>.Failure(new ApplicationError(ApplicationStatus.BusinessRuleViolation, msg, new
                            {
                                Range1 = dosagePerRoute[i].Crcl!,
                                Range2 = dosagePerRoute[j].Crcl!
                            }));
                        }
                    }
                }
            }

            return Result<bool>.Success(ApplicationStatus.Success, true);
        }
    }
}
