using AdsManager.Application.Common;
using AdsManager.Application.Configuration;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace AdsManager.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IInsightRepository _insightRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;

    public DashboardService(ITenantProvider tenantProvider, IInsightRepository insightRepository, ICacheService cacheService, IOptions<CacheOptions> cacheOptions)
    {
        _tenantProvider = tenantProvider;
        _insightRepository = insightRepository;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<Result<DashboardDto>> GetDashboardAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result<DashboardDto>.Fail("Tenant no resuelto");

        var cacheKey = InsightsCacheKeys.Dashboard(tenantId.Value, filter.DateFrom, filter.DateTo, filter.CampaignId, filter.AdAccountId);
        var ttl = TimeSpan.FromSeconds(Math.Max(1, _cacheOptions.DashboardTtlSeconds));

        var dashboard = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var totals = await _insightRepository.GetDashboardTotalsAsync(tenantId.Value, filter, ct);
                var topCampaigns = await _insightRepository.GetTopCampaignsAsync(tenantId.Value, filter, 5, ct);

                var averageCpm = totals.TotalImpressions == 0
                    ? 0
                    : decimal.Round(totals.TotalSpend / totals.TotalImpressions * 1000, 2);

                return new DashboardDto(
                    totals.TotalSpend,
                    totals.TotalImpressions,
                    totals.TotalClicks,
                    totals.AverageCtr,
                    totals.AverageCpc,
                    averageCpm,
                    topCampaigns);
            },
            ttl,
            cancellationToken);

        return Result<DashboardDto>.Ok(dashboard);
    }
}
