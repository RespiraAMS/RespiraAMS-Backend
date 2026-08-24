using Respira.ServiceDefaults.Constracts.CQRS;

namespace Respira.ServiceDefaults.Messages;

public record GetMediaQuery : IQuery
{
    public Guid Id { get; set; }
}
