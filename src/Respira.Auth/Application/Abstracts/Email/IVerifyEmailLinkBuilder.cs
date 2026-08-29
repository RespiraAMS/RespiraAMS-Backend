namespace Application.Abstracts.Email
{
    /// <summary>
    /// Builds the email verification link, resolving the public gateway base URL
    /// at call time from Aspire service discovery configuration
    /// </summary>
    public interface IVerifyEmailLinkBuilder
    {
        /// <summary>Builds the email verification link by substituting the token and email placeholders.</summary>
        /// <param name="token">Verification token to embed in the link.</param>
        /// <param name="email">Email address to embed in the link.</param>
        /// <returns>The fully-qualified verification URL.</returns>
        string Build(string token, string email);
    }
}
