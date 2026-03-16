using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class InsightRepository : IInsightRepository
{
    private readonly IApplicationDbContext _dbContext;

    public InsightRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<InsightDaily>> GetByDateRangeAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await _dbContext.InsightsDaily
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Date >= from && x.Date <= to)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

    public async Task<DashboardTotalsDto> GetDashboardTotalsAsync(Guid tenantId, DashboardFilter filter, CancellationToken cancellationToken = default)
    {
        var query = BuildDashboardQuery(tenantId, filter);

        var totals = await query
            .GroupBy(_ => 1)
            .Select(g => new DashboardTotalsDto(
                g.Sum(x => x.Spend),
                g.Sum(x => x.Impressions),
                g.Sum(x => x.Clicks),
                g.Average(x => x.Cpc),
                g.Average(x => x.Ctr)))
            .FirstOrDefaultAsync(cancellationToken);

        return totals ?? new DashboardTotalsDto(0, 0, 0, 0, 0);
    }

    public async Task<IReadOnlyCollection<TopCampaignDto>> GetTopCampaignsAsync(Guid tenantId, DashboardFilter filter, int take, CancellationToken cancellationToken = default)
        => await BuildDashboardQuery(tenantId, filter)
            .Where(x => x.CampaignId.HasValue)
            .Join(_dbContext.Campaigns.AsNoTracking(),
                insight => insight.CampaignId!.Value,
                campaign => campaign.Id,
                (insight, campaign) => new { campaign.Id, campaign.Name, insight.Spend, insight.Clicks, insight.Impressions })
            .GroupBy(x => new { x.Id, x.Name })
            .Select(g => new TopCampaignDto(
                g.Key.Id,
                g.Key.Name,
                g.Sum(x => x.Spend),
                g.Sum(x => x.Clicks),
                g.Sum(x => x.Impressions) == 0
                    ? 0
                    : decimal.Round((decimal)g.Sum(x => x.Clicks) / g.Sum(x => x.Impressions) * 100, 2)))
            .OrderByDescending(x => x.Spend)
            .Take(take)
            .ToArrayAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<InsightDaily> insights, CancellationToken cancellationToken = default)
    {
        _dbContext.InsightsDaily.AddRange(insights);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<InsightDaily> BuildDashboardQuery(Guid tenantId, DashboardFilter filter)
    {
        var query = _dbContext.InsightsDaily
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (filter.DateFrom.HasValue)
            query = query.Where(x => x.Date >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(x => x.Date <= filter.DateTo.Value);

        if (filter.CampaignId.HasValue)
            query = query.Where(x => x.CampaignId == filter.CampaignId.Value);

        if (filter.AdAccountId.HasValue)
            query = query.Where(x => x.AdAccountId == filter.AdAccountId.Value);

        return query;
    }
}
