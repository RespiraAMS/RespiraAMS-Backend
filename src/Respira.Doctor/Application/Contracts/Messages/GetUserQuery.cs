using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine.Attributes;

namespace Application.Contracts.Messages;

/// <summary>
/// Request for a doctor's auth account, sent to the Auth service via Wolverine
/// request/reply. Structurally identical to the Auth service's own <c>GetUserQuery</c>;
/// the shared <see cref="MessageIdentityAttribute"/> alias routes both types to the same
/// RabbitMQ exchange/queue without sharing a DTO assembly.
/// </summary>
[MessageIdentity("GetUserQuery")]
public record GetUserQuery : IQuery
{
    public Guid Id { get; set; }
}
