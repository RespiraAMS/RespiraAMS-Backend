using Respira.ServiceDefaults.Contracts.CQRS;

namespace Application.Features.Tokens.Commands.RemoveExpiredTokens;

/// <summary>
/// Command to revoke expired tokens: each expired token is moved into the blacklist
/// (mirroring <c>RemoveToken</c>) and removed from the tokens table. Expired blacklist
/// entries are also purged. Intended to be invoked periodically by a background service.
/// </summary>
public record RemoveExpiredTokensCommand : ICommand;
