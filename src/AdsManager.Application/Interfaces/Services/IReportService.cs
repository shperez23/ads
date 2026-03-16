using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;

namespace AdsManager.Application.Interfaces.Services;

public interface IReportService
{
    Task<Result<IReadOnlyCollection<InsightDto>>> GetInsightsAsync(DashboardFilter filter, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<InsightDto>>> GetCampaignInsightsAsync(Guid campaignId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<InsightDto>>> GetAdSetInsightsAsync(Guid adSetId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<InsightDto>>> GetAdInsightsAsync(Guid adId, DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default);
}
