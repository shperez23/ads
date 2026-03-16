namespace AdsManager.Application.Services;

public static class InsightsCacheKeys
{
    private const string Prefix = "insights:v1";

    public static string Dashboard(Guid tenantId, DateOnly? dateFrom, DateOnly? dateTo, Guid? campaignId, Guid? adAccountId)
        => $"{Prefix}:dashboard:{tenantId}:{Date(dateFrom)}:{Date(dateTo)}:{GuidOrDash(campaignId)}:{GuidOrDash(adAccountId)}";

    public static string Report(Guid tenantId, DateOnly? dateFrom, DateOnly? dateTo, Guid? campaignId, Guid? adAccountId)
        => $"{Prefix}:report:{tenantId}:{Date(dateFrom)}:{Date(dateTo)}:{GuidOrDash(campaignId)}:{GuidOrDash(adAccountId)}";

    public static string ReportPage(string reportBaseKey, int page, int pageSize, string? search, string? sortBy, string? sortDirection)
        => $"{reportBaseKey}:p:{page}:ps:{pageSize}:s:{Normalize(search)}:sb:{Normalize(sortBy)}:sd:{Normalize(sortDirection)}";

    public static IReadOnlyCollection<string> TenantPrefixes(Guid tenantId)
        =>
        [
            $"{Prefix}:dashboard:{tenantId}:",
            $"{Prefix}:report:{tenantId}:"
        ];

    private static string Date(DateOnly? value)
        => value?.ToString("yyyyMMdd") ?? "-";

    private static string GuidOrDash(Guid? value)
        => value?.ToString() ?? "-";

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim().ToLowerInvariant();
}
