using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine.Attributes;

namespace Application.Features.Media.Get.Queries;

/// <summary>
/// Query for a media asset by id. The <see cref="MessageIdentityAttribute"/> alias keeps
/// this type routable from other services (e.g. Doctor) that declare their own
/// structurally-identical copy of the message, without sharing a DTO assembly.
/// </summary>
[MessageIdentity("GetMediaQuery")]
public record GetMediaQuery : IQuery
{
    public Guid Id { get; set; }
}
