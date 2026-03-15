using AdsManager.Application.Interfaces;

namespace AdsManager.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string plainTextPassword) => BCrypt.Net.BCrypt.HashPassword(plainTextPassword);

    public bool Verify(string plainTextPassword, string hashedPassword) => BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
}
