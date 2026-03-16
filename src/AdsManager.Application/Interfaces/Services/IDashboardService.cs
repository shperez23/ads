using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Insights;

namespace AdsManager.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<Result<DashboardDto>> GetDashboardAsync(Guid tenantId, DashboardFilter filter, CancellationToken cancellationToken = default);
}
