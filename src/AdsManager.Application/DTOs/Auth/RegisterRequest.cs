namespace AdsManager.Application.DTOs.Auth;

public sealed record RegisterRequest(string TenantName, string TenantSlug, string Name, string Email, string Password);
