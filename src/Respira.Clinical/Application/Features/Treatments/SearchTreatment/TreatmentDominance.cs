namespace Respira.Clinical.Application.Features.Treatments.SearchTreatment
{
    /// <summary>
    /// Lightweight projection of a matched treatment, used to evaluate dominance
    /// between treatments without loading the whole entity graph
    /// </summary>
    /// <param name="Id">Treatment ID</param>
    /// <param name="PathogenIds">Pathogens this treatment covers</param>
    /// <param name="CriteriaIds">Criteria this treatment requires to be applicable</param>
    /// <param name="Solutions">Medicine compositions of this treatment</param>
    public sealed record TreatmentCandidate(
        Guid Id,
        List<Guid> PathogenIds,
        List<Guid> CriteriaIds,
        List<List<MedicineResult>> Solutions
    );

    /// <summary>
    /// Dominance filter for treatment search: hide a narrower treatment when a wider
    /// one is applicable, so that e.g. with pathogens [Pseudomonas, S. aureus] the
    /// Pseudomonas-only treatment is hidden in favour of the Pseudomonas + S. aureus one
    /// </summary>
    public static class TreatmentDominance
    {
        /// <summary>
        /// Drop treatment T when another matched U covers a strict superset of T's
        /// pathogens and U's criteria are a subset of T's criteria (U treats everything
        /// T treats, while being applicable whenever T is applicable). If no such U
        /// exists, the narrower treatment is kept as a fallback.
        /// </summary>
        /// <param name="candidates">Matched treatments before dominance</param>
        /// <param name="selectedPathogenIds">Pathogens selected in the search</param>
        /// <returns>Surviving treatments</returns>
        public static List<TreatmentCandidate> Filter(
            List<TreatmentCandidate> candidates,
            List<Guid> selectedPathogenIds
        )
        {
            // Dominance only makes sense when the search knows about a coinfection.
            // With 0 or 1 selected pathogens there is no "wider" treatment to prefer,
            // so every match is kept (fallback behaviour).
            if (selectedPathogenIds.Count < 2)
            {
                return [.. candidates];
            }

            return
            [
                .. candidates.Where(t =>
                    !candidates.Any(u =>
                        u.Id != t.Id
                        && u.PathogenIds.Count > t.PathogenIds.Count
                        && t.PathogenIds.All(p => u.PathogenIds.Contains(p))
                        && u.CriteriaIds.All(c => t.CriteriaIds.Contains(c))
                    )
                ),
            ];
        }
    }
}
