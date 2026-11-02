using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    /// <summary>
    /// An antibiotic group is a group of antibiotics that share common properties.
    /// A group can be a sub-category of another group (for example, monobactam and cephalosporin
    /// are both sub-categories of beta-lactam group). We call the super group the parent group
    /// in this context (clinical environment may not use the exact term, but the idea is the same)
    /// </summary>
    public class AntibioticGroup : FullTextSearch
    {
        /// <summary>
        /// Antibiotic group name
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Antibiotic group description
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Antibiotic group parent ID
        /// </summary>
        public required Guid? ParentId { get; set; }

        /// <summary>
        /// Antibiotic group parent
        /// </summary>
        public AntibioticGroup? Parent { get; set; }
    }
}
