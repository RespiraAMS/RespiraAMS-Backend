using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Domain.Models
{
    public record Diagnosis
    {
        /// <summary>
        /// Final severity diagnosis
        /// </summary>
        public required Severity Severity { get; set; }

        /// <summary>
        /// Final treatment site diagnosis
        /// </summary>
        public required TreatmentSite TreatmentSite { get; set; }

        /// <summary>
        /// This is the list of heavily suspected pathogens, based on risk factors.
        /// This list may be empty if no risk factors are met.
        /// If this list if not empty, then both engine and doctor should focus more
        /// on this list than the <see cref="WorthSuspected"/> list
        /// </summary>
        public required List<Pathogen> HeavySuspected { get; set; }

        /// <summary>
        /// This list contains pathogens that are worth considering, based on diagnosis
        /// of severity and treatment site. Since the only 2 criteria to judge is severity
        /// and treatment site based on past experience, this may not be accurate as
        /// <see cref="HeavySuspected"/> list.
        /// As long as data exists, this list should be non-empty
        /// </summary>
        public required List<Pathogen> WorthSuspected { get; set; }

        /// <summary>
        /// Evidences for the diagnosis result
        /// </summary>
        public required List<string> Evidences { get; set; }

        /// <summary>
        /// The list of missing variables when diagnosis is performed
        /// </summary>
        public required List<ClinicalVariable> MissingVariables { get; set; }

        /// <summary>
        /// The list of antibiotics that patient is allergic to. This is important,
        /// because the medicine recommendation MUST not have these allergic antibiotics
        /// </summary>
        public required List<Antibiotic> Allergies { get; set; }

        /// <summary>
        /// True if patient is pregnant or in lactation phase (female). This is important
        /// because some antibiotic may have negative effect on pregnant or lactating woman,
        /// so the final result MUST avoid them (to be more exact, antibiotics can be categorized
        /// based on the severity of side effects, and the final outcome would depend on the level
        /// assign to that antibiotic)
        /// </summary>
        public required bool IsPatientPregnantOrInLactationPhase { get; set; }
    }
}
