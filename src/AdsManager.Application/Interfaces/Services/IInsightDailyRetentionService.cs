namespace AdsManager.Application.Interfaces.Services;

public interface IInsightDailyRetentionService
{
    Task ApplyConfiguredPolicyAsync(CancellationToken cancellationToken = default);
}
