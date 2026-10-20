namespace Authentication.Application.Constracts.Email;

public sealed class EmailOption
{
    public const string SectionName = "Email";
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string UserName { get; init; }
    public required string Password { get; init; }
    public required string FromEmail { get; init; }
    public required string FromName { get; init; }
    public bool UseSsl { get; init; } = true;
}
