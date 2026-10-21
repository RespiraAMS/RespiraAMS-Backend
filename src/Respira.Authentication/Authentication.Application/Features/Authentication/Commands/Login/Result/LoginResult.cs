namespace Authentication.Application.Features.Authentication.Commands.Login.Result
{
    public record LoginResult
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }
}
