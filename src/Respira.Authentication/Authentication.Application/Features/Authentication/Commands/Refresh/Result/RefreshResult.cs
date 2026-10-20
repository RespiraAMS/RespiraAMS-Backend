namespace Authentication.Application.Features.Authentication.Commands.Refresh.Result
{
    /// <summary>
    /// Contains the replacement access and refresh tokens issued after a successful refresh.
    /// </summary>
    public record RefreshResult
    {
        /// <summary>
        /// Short-lived JWT used to authorize API requests.
        /// </summary>
        public required string AccessToken { get; init; }

        /// <summary>
        /// Replacement refresh token. The token supplied with the request is no longer valid.
        /// </summary>
        public required string RefreshToken { get; init; }
    }
}
