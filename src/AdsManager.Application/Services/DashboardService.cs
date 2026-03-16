using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;

namespace AdsManager.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IInsightRepository _insightRepository;
    private readonly ITenantProvider _tenantProvider;

    public DashboardService(ITenantProvider tenantProvider, IInsightRepository insightRepository)
    {
        _tenantProvider = tenantProvider;
        _insightRepository = insightRepository;
    }

    public async Task<Result<DashboardDto>> GetDashboardAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result<DashboardDto>.Fail("Tenant no resuelto");

        var totals = await _insightRepository.GetDashboardTotalsAsync(tenantId.Value, filter, cancellationToken);
        var topCampaigns = await _insightRepository.GetTopCampaignsAsync(tenantId.Value, filter, 5, cancellationToken);

        var averageCpm = totals.TotalImpressions == 0
            ? 0
            : decimal.Round(totals.TotalSpend / totals.TotalImpressions * 1000, 2);

        var dashboard = new DashboardDto(
            totals.TotalSpend,
            totals.TotalImpressions,
            totals.TotalClicks,
            totals.AverageCtr,
            totals.AverageCpc,
            averageCpm,
            topCampaigns);

        return Result<DashboardDto>.Ok(dashboard);
    }
}
