namespace AdsManager.Application.Services;

public static class InsightsCacheKeys
{
    private const string Prefix = "insights";

    public static string Dashboard(Guid tenantId, DateOnly? dateFrom, DateOnly? dateTo, Guid? campaignId, Guid? adAccountId)
        => $"{Prefix}:dashboard:{tenantId}:{dateFrom?.ToString("yyyyMMdd") ?? "-"}:{dateTo?.ToString("yyyyMMdd") ?? "-"}:{campaignId?.ToString() ?? "-"}:{adAccountId?.ToString() ?? "-"}";

    public static string Report(Guid tenantId, DateOnly? dateFrom, DateOnly? dateTo, Guid? campaignId, Guid? adAccountId)
        => $"{Prefix}:report:{tenantId}:{dateFrom?.ToString("yyyyMMdd") ?? "-"}:{dateTo?.ToString("yyyyMMdd") ?? "-"}:{campaignId?.ToString() ?? "-"}:{adAccountId?.ToString() ?? "-"}";

    public static IReadOnlyCollection<string> TenantPrefixes(Guid tenantId)
        =>
        [
            $"{Prefix}:dashboard:{tenantId}:",
            $"{Prefix}:report:{tenantId}:"
        ];
}
