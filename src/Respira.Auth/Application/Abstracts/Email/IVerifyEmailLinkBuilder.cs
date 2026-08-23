namespace Application.Abstracts.Email
{
    /// <summary>
    /// Builds the email verification link, resolving the public gateway base URL
    /// at call time from Aspire service discovery configuration
    /// </summary>
    public interface IVerifyEmailLinkBuilder
    {
        string Build(string token, string email);
    }
}
