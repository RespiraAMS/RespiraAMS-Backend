using Wolverine.Attributes;

namespace Application.Features.Treatments.GetTreatmentById
{
    [MessageIdentity("doctor-query")]
    public class GetDoctorByIdQuery(Guid id)
    {
        public Guid Id { get; set; } = id;
    }

    [MessageIdentity("doctor-result")]
    public record DoctorQueryResult
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string AcademicTitle { get; set; }
        public string? Url { get; set; }
    }
}
