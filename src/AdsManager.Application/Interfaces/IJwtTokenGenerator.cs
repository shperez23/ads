using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpirationUtc();
}
