namespace Application.Features.Doctors.Get.Results;

/// <summary>
/// Lightweight doctor projection for list views. Excludes the avatar media URL to
/// avoid an N+1 media lookup per row (expose <c>MediaId</c> if the caller needs it).
/// </summary>
public record DoctorListItemResult
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string Role { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public required string Status { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required ICollection<string> Degrees { get; set; }
    public required string AcademicTitle { get; set; }
    public required Domain.Enums.PositionType Position { get; set; }
    public ICollection<Guid>? Patients { get; set; }
    public required string Gender { get; set; }
    public required string CitizenIdentificationNumber { get; set; }
    public DateTimeOffset? DateOfBirth { get; set; }
    public required string Address { get; set; }
    public Guid? MediaId { get; set; }

    /// <summary>
    /// Doctors managed by this doctor (one level). Each item carries the same full
    /// info as a list row (local + auth-enriched) to support later CRUD operations.
    /// Null for leaf doctors; not recursively expanded.
    /// </summary>
    public ICollection<DoctorListItemResult>? Subordinates { get; set; }
}
