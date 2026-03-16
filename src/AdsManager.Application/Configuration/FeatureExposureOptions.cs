namespace AdsManager.Application.Configuration;

public sealed class FeatureExposureOptions
{
    public const string SectionName = "Features";

    public bool SwaggerEnabled { get; init; }
    public bool HangfireDashboardEnabled { get; init; }
    public bool ReadyHealthRequiresAuth { get; init; }
    public string[] HangfireDashboardIpAllowlist { get; init; } = [];
}
