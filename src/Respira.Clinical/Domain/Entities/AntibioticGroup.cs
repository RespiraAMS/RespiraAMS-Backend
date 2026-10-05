using Respira.ServiceDefaults.Models;

namespace Respira.Clinical.Domain.Entities
{
    public class AntibioticGroup : Base
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required Guid? ParentId { get; set; }
        public AntibioticGroup? Parent { get; set; }
    }
}
