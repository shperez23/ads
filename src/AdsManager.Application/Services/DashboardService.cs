using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _dbContext;

    public DashboardService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DashboardDto>> GetDashboardAsync(Guid tenantId, DashboardFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InsightsDaily.AsNoTracking().Where(x => x.TenantId == tenantId);

        if (filter.DateFrom.HasValue)
            query = query.Where(x => x.Date >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(x => x.Date <= filter.DateTo.Value);

        if (filter.CampaignId.HasValue)
            query = query.Where(x => x.CampaignId == filter.CampaignId.Value);

        if (filter.AdAccountId.HasValue)
            query = query.Where(x => x.AdAccountId == filter.AdAccountId.Value);

        var insights = await query.ToListAsync(cancellationToken);

        var totalSpend = insights.Sum(x => x.Spend);
        var totalImpressions = insights.Sum(x => x.Impressions);
        var totalClicks = insights.Sum(x => x.Clicks);
        var averageCtr = totalImpressions == 0 ? 0 : decimal.Round((decimal)totalClicks / totalImpressions * 100, 2);
        var averageCpc = totalClicks == 0 ? 0 : decimal.Round(totalSpend / totalClicks, 2);
        var averageCpm = totalImpressions == 0 ? 0 : decimal.Round(totalSpend / totalImpressions * 1000, 2);

        var topCampaigns = await _dbContext.Campaigns
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Join(insights.Where(x => x.CampaignId.HasValue), c => c.Id, i => i.CampaignId!.Value,
                (campaign, insight) => new { campaign.Id, campaign.Name, insight.Spend, insight.Clicks, insight.Impressions })
            .GroupBy(x => new { x.Id, x.Name })
            .Select(g => new TopCampaignDto(
                g.Key.Id,
                g.Key.Name,
                g.Sum(x => x.Spend),
                g.Sum(x => x.Clicks),
                g.Sum(x => x.Impressions) == 0 ? 0 : decimal.Round((decimal)g.Sum(x => x.Clicks) / g.Sum(x => x.Impressions) * 100, 2)))
            .OrderByDescending(x => x.Spend)
            .Take(5)
            .ToArrayAsync(cancellationToken);

        var dashboard = new DashboardDto(totalSpend, totalImpressions, totalClicks, averageCtr, averageCpc, averageCpm, topCampaigns);
        return Result<DashboardDto>.Ok(dashboard);
    }
}
