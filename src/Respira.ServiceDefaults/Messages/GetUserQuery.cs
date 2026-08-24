using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.ServiceDefaults.Messages;

public record GetUserQuery : IQuery
{
    public Guid Id { get; set; }
}
