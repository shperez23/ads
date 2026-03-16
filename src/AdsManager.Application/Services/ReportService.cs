using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;

    public ReportService(IApplicationDbContext dbContext, ITenantProvider tenantProvider)
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyCollection<InsightDto>>> GetInsightsAsync(DashboardFilter filter, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Result<IReadOnlyCollection<InsightDto>>.Fail("Tenant no resuelto");

        var query = BuildInsightsQuery(filter.DateFrom, filter.DateTo);

        if (filter.CampaignId.HasValue)
            query = query.Where(x => x.CampaignId == filter.CampaignId.Value);

        if (filter.AdAccountId.HasValue)
            query = query.Where(x => x.AdAccountId == filter.AdAccountId.Value);

        var insights = await query.OrderByDescending(x => x.Date).ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<InsightDto>>.Ok(MapInsights(insights));
    }

    public async Task<Result<IReadOnlyCollection<InsightDto>>> GetCampaignInsightsAsync(Guid campaignId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Result<IReadOnlyCollection<InsightDto>>.Fail("Tenant no resuelto");

        var exists = await _dbContext.Campaigns.AsNoTracking().AnyAsync(x => x.Id == campaignId, cancellationToken);
        if (!exists)
            return Result<IReadOnlyCollection<InsightDto>>.Fail("Campaign no encontrada");

        var insights = await BuildInsightsQuery(dateFrom, dateTo)
            .Where(x => x.CampaignId == campaignId)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<InsightDto>>.Ok(MapInsights(insights));
    }

    public async Task<Result<IReadOnlyCollection<InsightDto>>> GetAdSetInsightsAsync(Guid adSetId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Result<IReadOnlyCollection<InsightDto>>.Fail("Tenant no resuelto");

        var exists = await _dbContext.AdSets.AsNoTracking().AnyAsync(x => x.Id == adSetId, cancellationToken);
        if (!exists)
            return Result<IReadOnlyCollection<InsightDto>>.Fail("AdSet no encontrado");

        var insights = await BuildInsightsQuery(dateFrom, dateTo)
            .Where(x => x.AdSetId == adSetId)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<InsightDto>>.Ok(MapInsights(insights));
    }

    public async Task<Result<IReadOnlyCollection<InsightDto>>> GetAdInsightsAsync(Guid adId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Result<IReadOnlyCollection<InsightDto>>.Fail("Tenant no resuelto");

        var exists = await _dbContext.Ads.AsNoTracking().AnyAsync(x => x.Id == adId, cancellationToken);
        if (!exists)
            return Result<IReadOnlyCollection<InsightDto>>.Fail("Ad no encontrado");

        var insights = await BuildInsightsQuery(dateFrom, dateTo)
            .Where(x => x.AdId == adId)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<InsightDto>>.Ok(MapInsights(insights));
    }

    private IQueryable<Domain.Entities.InsightDaily> BuildInsightsQuery(DateOnly? dateFrom, DateOnly? dateTo)
    {
        var query = _dbContext.InsightsDaily.AsNoTracking();

        if (dateFrom.HasValue)
            query = query.Where(x => x.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.Date <= dateTo.Value);

        return query;
    }

    private static IReadOnlyCollection<InsightDto> MapInsights(IReadOnlyCollection<Domain.Entities.InsightDaily> insights)
        => insights
            .Select(x => new InsightDto(x.Id, x.AdAccountId, x.CampaignId, x.AdSetId, x.AdId, x.Date, x.Impressions, x.Reach, x.Clicks, x.LinkClicks, x.Spend, x.Cpm, x.Cpc, x.Ctr))
            .ToArray();
}
