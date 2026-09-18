using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    /// <summary>
    /// Pathogen: cause of the disease, which can either be a virus, bacteria or fungi
    /// </summary>
    public class Pathogen : Base
    {
        /// <summary>
        /// Pathogen name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Pathogen description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Boolean flag to indicate if the pathogen is atypical.
        /// An atypical pathogen is a pathogen that lack a normal cell wall,
        /// which make Beta-lactam antibiotics ineffective against them.
        /// </summary>
        public required bool IsAtypical { get; set; }

        /// <summary>
        /// Pathogen risk factors
        /// </summary>
        public ICollection<RiskFactor> RiskFactors { get; set; } = [];
    }
}
