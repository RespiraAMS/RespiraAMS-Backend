namespace Authentication.Application.Constracts.Authentication
{
    public class JwtOption
    {
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public required string SecretKey { get; set; }
        public required long AccessTokenExpires { get; set; }
        public required long RefreshTokenExpires { get; set; }
    }
}
