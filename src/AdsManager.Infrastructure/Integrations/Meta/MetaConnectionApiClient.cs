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
}
