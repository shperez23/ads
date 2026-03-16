namespace AdsManager.Application.Interfaces.Meta;

public interface IMetaConnectionApiClient
{
    Task<(bool IsTokenValid, IReadOnlyCollection<string> GrantedPermissions)> ValidateTokenAndPermissionsAsync(string appId, string appSecret, string accessToken, CancellationToken cancellationToken = default);
}
