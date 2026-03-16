namespace AdsManager.Application.Configuration;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";
    public int DashboardTtlSeconds { get; set; } = 120;
    public int ReportTtlSeconds { get; set; } = 120;
}
