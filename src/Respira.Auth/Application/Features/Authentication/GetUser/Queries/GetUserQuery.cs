using Respira.ServiceDefaults.Constracts.CQRS;
using Wolverine.Attributes;

namespace Application.Features.Authentication.GetUser.Queries;

/// <summary>
/// Query for a doctor's auth account by id. The <see cref="MessageIdentityAttribute"/>
/// alias keeps this type routable from other services (e.g. Doctor) that declare their
/// own structurally-identical copy of the message, without sharing a DTO assembly.
/// </summary>
[MessageIdentity("GetUserQuery")]
public record GetUserQuery : IQuery
{
    public Guid Id { get; set; }
}
