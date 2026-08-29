using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine.Attributes;

namespace Application.Features.Authentication.GetListInfoDoctor.Queries;

/// <summary>
/// Batch query for auth-side details of a set of doctors, used by the Doctor
/// service to enrich a doctor list without N+1 calls. The
/// <see cref="MessageIdentityAttribute"/> alias lets the Doctor service declare a
/// structurally-identical copy routed to the same RabbitMQ exchange without
/// sharing a DTO assembly.
/// </summary>
[MessageIdentity("GetListInfoDoctorQuery")]
public record GetListInfoDoctorQuery : IQuery
{
    /// <summary>Identifiers of the doctors whose auth details are requested.</summary>
    public required List<Guid> Ids { get; init; }
}
