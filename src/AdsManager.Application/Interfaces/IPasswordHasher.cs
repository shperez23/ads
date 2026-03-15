namespace AdsManager.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string hashedPassword);
}
