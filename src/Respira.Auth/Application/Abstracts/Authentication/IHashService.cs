namespace Application.Abstracts.Authentication
{
    public interface IHashService
    {
        public string HashPassword(string password);
        public bool VerifyPassword(string password, string hashedPassword);
        public string HashToken(string token);
        public bool VerifyToken(string token, string hashedToken);
    }
}
