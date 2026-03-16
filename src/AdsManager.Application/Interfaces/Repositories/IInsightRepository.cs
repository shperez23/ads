using AdsManager.Domain.Entities;

namespace AdsManager.Application.Interfaces.Repositories;

public interface IInsightRepository
{
    Task<IReadOnlyCollection<InsightDaily>> GetByDateRangeAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<InsightDaily> insights, CancellationToken cancellationToken = default);
}
