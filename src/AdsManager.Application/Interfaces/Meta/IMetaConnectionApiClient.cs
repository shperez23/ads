namespace AdsManager.Application.Interfaces.Meta;

public interface IMetaConnectionApiClient
{
    Task<(bool IsTokenValid, IReadOnlyCollection<string> GrantedPermissions)> ValidateTokenAndPermissionsAsync(string appId, string appSecret, string accessToken, CancellationToken cancellationToken = default);
    Task<MetaTokenRefreshApiResult> TryRefreshTokenAsync(string appId, string appSecret, string accessToken, CancellationToken cancellationToken = default);
}

public sealed record MetaTokenRefreshApiResult(bool Success, bool IsSupported, string? AccessToken, DateTime? ExpiresAtUtc, string Message, int StatusCode = 0, string ResponsePayload = "{}");
