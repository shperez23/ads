using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IApplicationDbContext _dbContext;

    public ReportService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyCollection<InsightDto>>> GetInsightsAsync(Guid tenantId, DashboardFilter filter, CancellationToken cancellationToken = default)
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

        var insights = await query.OrderByDescending(x => x.Date).ToListAsync(cancellationToken);

        var response = insights
            .Select(x => new InsightDto(x.Id, x.AdAccountId, x.CampaignId, x.AdSetId, x.AdId, x.Date, x.Impressions, x.Reach, x.Clicks, x.LinkClicks, x.Spend, x.Cpm, x.Cpc, x.Ctr))
            .ToArray();

        return Result<IReadOnlyCollection<InsightDto>>.Ok(response);
    }
}
