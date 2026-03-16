namespace AdsManager.API.Authorization;

public static class AuthorizationPolicies
{
    public const string CampaignsRead = "Campaigns.Read";
    public const string CampaignsWrite = "Campaigns.Write";
    public const string AdSetsRead = "AdSets.Read";
    public const string AdSetsWrite = "AdSets.Write";
    public const string AdsRead = "Ads.Read";
    public const string AdsWrite = "Ads.Write";
    public const string ReportsRead = "Reports.Read";
    public const string MetaConnectionsManage = "MetaConnections.Manage";
    public const string AdAccountsManage = "AdAccounts.Manage";
    public const string SystemAdmin = "System.Admin";

    public static readonly string[] All =
    [
        CampaignsRead,
        CampaignsWrite,
        AdSetsRead,
        AdSetsWrite,
        AdsRead,
        AdsWrite,
        ReportsRead,
        MetaConnectionsManage,
        AdAccountsManage,
        SystemAdmin
    ];
}
