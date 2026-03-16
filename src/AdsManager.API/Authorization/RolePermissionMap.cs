using AdsManager.Domain.Enums;

namespace AdsManager.API.Authorization;

public static class RolePermissionMap
{
    private static readonly IReadOnlyDictionary<string, HashSet<string>> PermissionsByRole =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [UserRole.Admin.ToString()] =
            [
                AuthorizationPolicies.CampaignsRead,
                AuthorizationPolicies.CampaignsWrite,
                AuthorizationPolicies.AdSetsRead,
                AuthorizationPolicies.AdSetsWrite,
                AuthorizationPolicies.AdsRead,
                AuthorizationPolicies.AdsWrite,
                AuthorizationPolicies.ReportsRead,
                AuthorizationPolicies.MetaConnectionsManage,
                AuthorizationPolicies.AdAccountsManage,
                AuthorizationPolicies.RulesRead,
                AuthorizationPolicies.RulesWrite,
                AuthorizationPolicies.SystemAdmin
            ],
            [UserRole.Manager.ToString()] =
            [
                AuthorizationPolicies.CampaignsRead,
                AuthorizationPolicies.CampaignsWrite,
                AuthorizationPolicies.AdSetsRead,
                AuthorizationPolicies.AdSetsWrite,
                AuthorizationPolicies.AdsRead,
                AuthorizationPolicies.AdsWrite,
                AuthorizationPolicies.ReportsRead,
                AuthorizationPolicies.MetaConnectionsManage,
                AuthorizationPolicies.AdAccountsManage,
                AuthorizationPolicies.RulesRead,
                AuthorizationPolicies.RulesWrite
            ],
            [UserRole.Analyst.ToString()] =
            [
                AuthorizationPolicies.CampaignsRead,
                AuthorizationPolicies.AdSetsRead,
                AuthorizationPolicies.AdsRead,
                AuthorizationPolicies.ReportsRead,
                AuthorizationPolicies.RulesRead
            ]
        };

    public static bool RoleHasPermission(string role, string permission)
        => PermissionsByRole.TryGetValue(role, out var permissions)
           && permissions.Contains(permission);
}
