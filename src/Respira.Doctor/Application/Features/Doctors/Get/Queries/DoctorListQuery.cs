using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Dtos;

namespace Application.Features.Doctors.Get.Queries;

/// <summary>
/// Filter for the doctor list (profile-level search).
/// </summary>
public class DoctorListFilter
{
    /// <summary>Optional case-insensitive search across first/last name.</summary>
    public string? Search { get; set; }
}

/// <summary>
/// Query for a paged list of doctors (profile level) enriched with auth-side
/// details. Pagination follows the shared <see cref="PaginationParam"/> convention;
/// auth details are fetched in a single batched call to the Auth service.
/// </summary>
public record DoctorListQuery : IQuery
{
    /// <summary>Shared pagination parameters.</summary>
    public required PaginationParam Param { get; set; } = null!;

    /// <summary>Optional list filter.</summary>
    public DoctorListFilter? Filter { get; set; }
}
