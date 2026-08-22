namespace Application.Abstracts.Email
{
    /// <summary>
    /// Configuration for the email verification link
    /// </summary>
    public class VerifyEmailOption
    {
        /// <summary>
        /// Link template to verify email, supports {token} and {email} placeholders.
        /// Example: https://gateway.example.com/api/v1/auth/verify-email?token={token}&email={email}
        /// </summary>
        public required string LinkTemplate { get; set; }
    }
}