using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Application.Interfaces.Services;

namespace AdsManager.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IInsightRepository _insightRepository;

    public ReportService(IInsightRepository insightRepository)
    {
        _insightRepository = insightRepository;
    }

    public async Task<Result<IReadOnlyCollection<InsightDto>>> GetInsightsAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (from > to)
            return Result<IReadOnlyCollection<InsightDto>>.Fail("Rango de fechas inválido");

        var insights = await _insightRepository.GetByDateRangeAsync(tenantId, from, to, cancellationToken);

        var response = insights
            .Select(x => new InsightDto(x.Id, x.AdAccountId, x.CampaignId, x.AdSetId, x.AdId, x.Date, x.Impressions, x.Reach, x.Clicks, x.LinkClicks, x.Spend, x.Cpm, x.Cpc, x.Ctr))
            .ToArray();

        return Result<IReadOnlyCollection<InsightDto>>.Ok(response);
    }
}
