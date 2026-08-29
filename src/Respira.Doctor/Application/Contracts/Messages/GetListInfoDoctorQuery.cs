using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine.Attributes;

namespace Application.Contracts.Messages;

/// <summary>
/// Batch request for auth-side doctor details, sent to the Auth service via
/// Wolverine request/reply. Structurally identical to the Auth service's own
/// <c>GetListInfoDoctorQuery</c>; the shared <see cref="MessageIdentityAttribute"/>
/// alias routes both types to the same RabbitMQ exchange without sharing a DTO assembly.
/// </summary>
[MessageIdentity("GetListInfoDoctorQuery")]
public record GetListInfoDoctorQuery : IQuery
{
    /// <summary>Identifiers of the doctors whose auth details are requested.</summary>
    public required List<Guid> Ids { get; init; }
}
