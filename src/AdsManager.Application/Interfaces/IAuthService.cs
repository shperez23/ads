using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Auth;

namespace AdsManager.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserProfileDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
