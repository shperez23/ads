namespace AdsManager.Application.Configuration;

public sealed class AuthProtectionOptions
{
    public const string SectionName = "AuthProtection";

    public int LoginPerMinute { get; set; } = 10;
    public int RefreshPerMinute { get; set; } = 20;
    public int RegisterPerHour { get; set; } = 5;
    public int FailedAttemptsThreshold { get; set; } = 5;
    public int FailedAttemptsWindowMinutes { get; set; } = 15;
    public int LockoutBaseMinutes { get; set; } = 5;
    public int LockoutMaxMinutes { get; set; } = 60;
}
