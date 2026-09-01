using Respira.ServiceDefaults.Contracts.CQRS;
using Wolverine.Attributes;

namespace Application.Contracts.Messages;

/// <summary>
/// Request for a media asset URL, sent to the Media service via Wolverine
/// request/reply. Structurally identical to the Media service's own <c>GetMediaQuery</c>;
/// the shared <see cref="MessageIdentityAttribute"/> alias routes both types to the same
/// RabbitMQ exchange/queue without sharing a DTO assembly.
/// </summary>
[MessageIdentity("GetMediaQuery")]
public record GetMediaQuery : IQuery
{
    public Guid Id { get; set; }
}
