namespace Application.Features.Authentication.Login.Result
{
    /// <summary>
    /// Result of a successful login: the issued access and refresh tokens.
    /// </summary>
    public record LoginResult
    {
        /// <summary>JWT access token</summary>
        public required string AccessToken { get; set; }

        /// <summary>JWT refresh token (persisted for revocation)</summary>
        public required string RefreshToken { get; set; }
    }
}
