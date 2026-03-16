using AdsManager.Application.Common;
using AdsManager.Application.Configuration;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AdsManager.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IInsightRepository _insightRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;

    public ReportService(IApplicationDbContext dbContext, IInsightRepository insightRepository, ITenantProvider tenantProvider, ICacheService cacheService, IOptions<CacheOptions> cacheOptions)
    {
        _dbContext = dbContext;
        _insightRepository = insightRepository;
        _tenantProvider = tenantProvider;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<Result<PagedResponse<InsightDto>>> GetInsightsAsync(InsightListRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Result<PagedResponse<InsightDto>>.Fail("Tenant no resuelto");

        var tenantId = _tenantProvider.GetTenantId()!.Value;
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize;
        var reportBaseKey = InsightsCacheKeys.Report(tenantId, request.DateFrom, request.DateTo, request.CampaignId, request.AdAccountId);
        var cacheKey = InsightsCacheKeys.ReportPage(reportBaseKey, page, pageSize, request.Search, request.SortBy, request.SortDirection.ToString());
        var ttl = TimeSpan.FromSeconds(Math.Max(1, _cacheOptions.ReportTtlSeconds));

        var response = await _cacheService.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var (items, total) = await _insightRepository.GetPagedAsync(tenantId, request, ct);
                var totalPages = (int)Math.Ceiling(total / (double)pageSize);
                return new PagedResponse<InsightDto>(MapInsights(items), page, pageSize, total, totalPages);
            },
            ttl,
            cancellationToken);

        return Result<PagedResponse<InsightDto>>.Ok(response);
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
