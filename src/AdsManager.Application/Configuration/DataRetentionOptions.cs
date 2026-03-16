namespace AdsManager.Application.Configuration;

public sealed class DataRetentionOptions
{
    public const string SectionName = "DataRetention";

    public int ApiLogsDays { get; set; } = 30;
    public int AuditLogsDays { get; set; } = 180;
    public int RuleExecutionLogsDays { get; set; } = 90;
    public int SyncJobRunsDays { get; set; } = 90;
    public InsightDailyRetentionOptions InsightDaily { get; set; } = new();
}

public sealed class InsightDailyRetentionOptions
{
    public bool Enabled { get; set; }
    public string Mode { get; set; } = "None";
    public int? GlobalRetentionDays { get; set; }
    public List<InsightDailyTenantRetentionPolicy> TenantPolicies { get; set; } = [];
}

public sealed class InsightDailyTenantRetentionPolicy
{
    public Guid TenantId { get; set; }
    public int? RetentionDays { get; set; }
    public DateOnly? PurgeBeforeDate { get; set; }
}
