using System.Text.Json;
using AdsManager.Application.Interfaces.Meta;

namespace AdsManager.Infrastructure.Integrations.Meta;

public sealed class MetaConnectionApiClient : IMetaConnectionApiClient
{
    private const string BaseUrl = "https://graph.facebook.com/v19.0/";
    private readonly HttpClient _httpClient;

    public MetaConnectionApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<(bool IsTokenValid, IReadOnlyCollection<string> GrantedPermissions)> ValidateTokenAndPermissionsAsync(string appId, string appSecret, string accessToken, CancellationToken cancellationToken = default)
    {
        var appToken = $"{appId}|{appSecret}";
        var debugTokenEndpoint = $"debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appToken)}";

        using var debugResponse = await _httpClient.GetAsync(debugTokenEndpoint, cancellationToken);
        var debugJson = await debugResponse.Content.ReadAsStringAsync(cancellationToken);
        debugResponse.EnsureSuccessStatusCode();

        using var debugDoc = JsonDocument.Parse(debugJson);
        var isValid = debugDoc.RootElement
            .GetProperty("data")
            .TryGetProperty("is_valid", out var validElement)
            && validElement.GetBoolean();

        var permissions = new List<string>();
        if (isValid)
        {
            var permissionsEndpoint = $"me/permissions?access_token={Uri.EscapeDataString(accessToken)}";
            using var permissionsResponse = await _httpClient.GetAsync(permissionsEndpoint, cancellationToken);
            var permissionsJson = await permissionsResponse.Content.ReadAsStringAsync(cancellationToken);
            permissionsResponse.EnsureSuccessStatusCode();

            using var permissionsDoc = JsonDocument.Parse(permissionsJson);
            if (permissionsDoc.RootElement.TryGetProperty("data", out var dataElement))
            {
                foreach (var permissionItem in dataElement.EnumerateArray())
                {
                    var status = permissionItem.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : string.Empty;
                    if (!string.Equals(status, "granted", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var permissionName = permissionItem.TryGetProperty("permission", out var permissionElement)
                        ? permissionElement.GetString()
                        : string.Empty;

                    if (!string.IsNullOrWhiteSpace(permissionName))
                        permissions.Add(permissionName);
                }
            }
        }

        return (isValid, permissions);
    }

    public async Task<MetaTokenRefreshApiResult> TryRefreshTokenAsync(string appId, string appSecret, string accessToken, CancellationToken cancellationToken = default)
    {
        var endpoint = $"oauth/access_token?grant_type=fb_exchange_token&client_id={Uri.EscapeDataString(appId)}&client_secret={Uri.EscapeDataString(appSecret)}&fb_exchange_token={Uri.EscapeDataString(accessToken)}";

        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new MetaTokenRefreshApiResult(
                Success: false,
                IsSupported: true,
                AccessToken: null,
                ExpiresAtUtc: null,
                Message: "Meta token refresh failed.",
                StatusCode: (int)response.StatusCode,
                ResponsePayload: string.IsNullOrWhiteSpace(responseJson) ? "{}" : responseJson);
        }

        using var doc = JsonDocument.Parse(responseJson);
        var refreshedToken = doc.RootElement.TryGetProperty("access_token", out var tokenElement)
            ? tokenElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(refreshedToken))
        {
            return new MetaTokenRefreshApiResult(
                Success: false,
                IsSupported: true,
                AccessToken: null,
                ExpiresAtUtc: null,
                Message: "Meta did not return an access token.",
                StatusCode: (int)response.StatusCode,
                ResponsePayload: responseJson);
        }

        DateTime? expiresAt = null;
        if (doc.RootElement.TryGetProperty("expires_in", out var expiresInElement)
            && expiresInElement.ValueKind == JsonValueKind.Number
            && expiresInElement.TryGetInt64(out var expiresInSeconds)
            && expiresInSeconds > 0)
        {
            expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        }

        return new MetaTokenRefreshApiResult(
            Success: true,
            IsSupported: true,
            AccessToken: refreshedToken,
            ExpiresAtUtc: expiresAt,
            Message: "Meta token refreshed successfully.",
            StatusCode: (int)response.StatusCode,
            ResponsePayload: responseJson);
    }
}
