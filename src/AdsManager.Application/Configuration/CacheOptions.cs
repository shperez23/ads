namespace AdsManager.Application.Configuration;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public string Provider { get; set; } = "Memory";
    public int DashboardTtlSeconds { get; set; } = 120;
    public int ReportTtlSeconds { get; set; } = 120;
    public RedisCacheOptions Redis { get; set; } = new();
}

public sealed class RedisCacheOptions
{
    public string? ConnectionString { get; set; }
    public string? InstanceName { get; set; }
    public int DefaultTtlSeconds { get; set; } = 120;
}
