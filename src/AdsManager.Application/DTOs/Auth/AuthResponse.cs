namespace AdsManager.Application.DTOs.Auth;

public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserProfileDto User);

public sealed record UserProfileDto(Guid Id, Guid TenantId, string Name, string Email, string Role);
