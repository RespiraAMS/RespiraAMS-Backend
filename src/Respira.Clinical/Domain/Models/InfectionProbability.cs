using Respira.Domain.Entities;

namespace Respira.Domain.Models
{
    /// <summary>
    /// This record is used to indicate pathogen with probability for infection,
    /// based on its risk factors
    /// </summary>
    /// <param name="Pathogen">The suspected pathogen for infection</param>
    /// <param name="Probability">Infection probability (from 0 to 1)</param>
    public record HeavySuspected(Pathogen Pathogen, decimal Probability);

    /// <summary>
    /// Infection probability result
    /// </summary>
    public class InfectionProbability
    {
        /// <summary>
        /// This is the list of heavily suspected pathogens, based on risk factors.
        /// This list may be empty if no risk factors are met.
        /// If this list if not empty, then both engine and doctor should focus more
        /// on this list than the <see cref="WorthSuspected"/> list
        /// </summary>
        public IEnumerable<HeavySuspected> HeavySuspected { get; set; } = [];

        /// <summary>
        /// This list contains pathogens that are worth considering, based on diagnosis
        /// of severity and treatment site. Since the only 2 criteria to judge is severity
        /// and treatment site based on past experience, this may not be accurate as
        /// <see cref="HeavySuspected"/> list.
        /// As long as data exists, this list should be non-empty
        /// </summary>
        public required IEnumerable<Pathogen> WorthSuspected { get; set; }
    }
}
