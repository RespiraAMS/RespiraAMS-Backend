using Microsoft.Extensions.Logging;

namespace Application.Features.Antibiotics.Shared;

public class DosageBusinessChecker(ILogger<DosageBusinessChecker> logger)
{
    public bool IsValidDosage(List<Dosage> dosages)
    {
        // Antibiotic dosage should adhere to these rules
        // 1. There must be at least 1 dosage regardless of route of administration
        // 2. For each route of administration (if exists), there must be 1 and only 1
        // standard dose (CrCl == null)
        // 3. For each route, CrCl must not overlapped with any other CrCl

        dosages = [.. dosages]; // Deep copy to avoid any EF tracking issue

        // Check rule 1
        if (!dosages.Any())
        {
            logger.LogDebug("Dosage is empty, dosage must not empty");
            return false;
        }

        foreach (var route in dosages.Select(d => d.RouteOfAdministration).Distinct().ToList())
        {
            var dosagePerRoute = dosages
                .Where(d => d.RouteOfAdministration == route)
                .ToList();

            // Check rule 2
            if (dosagePerRoute.Count(d => d.Crcl == null) != 1)
            {
                logger.LogDebug("There is no standard dose for route {route}", route);
                return false;
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
                        logger.LogDebug("Route {route} has dosage CrCl overlapped", route);
                        return false;
                    }
                }
            }
        }

        return true;
    }
}
