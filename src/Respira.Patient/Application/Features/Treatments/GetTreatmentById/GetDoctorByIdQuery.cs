using Wolverine.Attributes;

namespace Application.Features.Treatments.GetTreatmentById
{
    [MessageIdentity("doctor-query")]
    public record GetDoctorByIdQuery(Guid Id)
    {
        public Guid Id { get; set; } = Id;
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
