using System.Security.Cryptography;
using AdsManager.Application.Interfaces;

namespace AdsManager.Infrastructure.Security;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string refreshToken)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    public bool VerifyToken(string refreshToken, string hashedToken)
    {
        var computedHash = HashToken(refreshToken);
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(computedHash),
            System.Text.Encoding.UTF8.GetBytes(hashedToken));
    }
}
