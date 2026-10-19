using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Domain.Models
{
    /// <summary>
    /// Scored pathogen, pathogen with score when assessing infection
    /// </summary>
    /// <param name="Score">Score</param>
    /// <param name="Pathogen">Pathogen</param>
    public record ScoredPathogen(decimal Score, Pathogen Pathogen);

    /// <summary>
    /// Infection assessment result
    /// </summary>
    public class InfectionAssessment
    {
        /// <summary>
        /// This is the list of heavily suspected pathogens, based on risk factors.
        /// This list may be empty if no risk factors are met.
        /// If this list is not empty, then both engine and doctor should focus more
        /// on this list than the <see cref="WorthSuspected"/> list
        /// </summary>
        public required IEnumerable<ScoredPathogen> HeavySuspected { get; set; }

        /// <summary>
        /// This list contains pathogens that are worth considering, based on diagnosis
        /// of severity and treatment site. Since the only 2 criteria to judge is severity
        /// and treatment site based on experience, this may not be accurate as
        /// <see cref="HeavySuspected"/> list
        /// As long as data exists, this list should be non-empty
        /// </summary>
        public required IEnumerable<Pathogen> WorthSuspected { get; set; }

        public required IEnumerable<Evidence> Evidences { get; set; }
    }
}
