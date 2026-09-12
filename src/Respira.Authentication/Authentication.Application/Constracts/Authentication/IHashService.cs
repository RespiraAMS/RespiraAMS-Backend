namespace Authentication.Application.Constracts.Authentication
{
    public interface IHashService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
        string HashToken(string token);
        bool VerifyToken(string token, string hash);
    }
}
