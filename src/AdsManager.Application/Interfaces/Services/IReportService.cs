using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;

namespace AdsManager.Application.Interfaces.Services;

public interface IReportService
{
    Task<Result<IReadOnlyCollection<InsightDto>>> GetInsightsAsync(Guid tenantId, DashboardFilter filter, CancellationToken cancellationToken = default);
}
