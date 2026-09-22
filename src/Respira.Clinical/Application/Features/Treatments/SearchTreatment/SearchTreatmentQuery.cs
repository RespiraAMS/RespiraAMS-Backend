using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Clinical.Application.Features.Treatments.SearchTreatment
{
    public record SearchTreatmentQuery : IQuery
    {
        public required Severity Severity { get; set; }
        public required TreatmentSite TreatmentSite { get; set; }
        public List<Guid> PathogenIds { get; set; } = [];
        public List<Guid> CriteriaIds { get; set; } = [];
    }

    public record MedicineResult(Guid Id, string Name, string AntibioticGroupName);

    public record TreatmentMedicineResult(IEnumerable<MedicineResult> Medicines);

    public record SearchTreatmentResult(IEnumerable<TreatmentMedicineResult> Solutions);
}
