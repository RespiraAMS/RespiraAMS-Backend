using Respira.Clinical.Domain.Entities;

namespace Respira.Clinical.Domain.Models
{
    /// <summary>
    /// This record is used to indicate pathogen with probability for infection,
    /// based on its risk factors
    /// </summary>
    /// <param name="Pathogen">The suspected pathogen for infection</param>
    /// <param name="PriorityScore">
    /// Priority score. Note that, this score does not indicate the probability of infection,
    /// this is simply used to rank the pathogens.
    /// </param>
    public record HeavySuspected(Pathogen Pathogen, decimal PriorityScore);

    /// <summary>
    /// Infection assessment result
    /// </summary>
    public class InfectionAssessment
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
