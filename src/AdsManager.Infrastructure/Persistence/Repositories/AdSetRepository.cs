using AdsManager.Application.DTOs.AdSets;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class AdSetRepository : IAdSetRepository
{
    private readonly IApplicationDbContext _dbContext;

    public AdSetRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<AdSet>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.AdSets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyCollection<AdSet> Items, int Total)> GetPagedByTenantAsync(Guid tenantId, AdSetListRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AdSets.AsNoTracking().Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.MetaAdSetId.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Status == request.Status);

        if (request.CampaignId.HasValue)
            query = query.Where(x => x.CampaignId == request.CampaignId.Value);

        query = ApplySorting(query, request.SortBy, request.SortDirection);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.NormalizedPage - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<AdSet?> GetByIdAsync(Guid tenantId, Guid adSetId, CancellationToken cancellationToken = default)
        => _dbContext.AdSets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == adSetId, cancellationToken);

    public Task<AdSet?> GetByMetaAdSetIdAsync(Guid tenantId, string metaAdSetId, CancellationToken cancellationToken = default)
        => _dbContext.AdSets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaAdSetId == metaAdSetId, cancellationToken);

    public async Task AddAsync(AdSet adSet, CancellationToken cancellationToken = default)
    {
        _dbContext.AdSets.Add(adSet);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AdSet adSet, CancellationToken cancellationToken = default)
    {
        _dbContext.AdSets.Update(adSet);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<AdSet> ApplySorting(IQueryable<AdSet> query, string? sortBy, SortDirection sortDirection)
    {
        var desc = sortDirection == SortDirection.Desc;

        return sortBy?.ToLowerInvariant() switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "budget" => desc ? query.OrderByDescending(x => x.Budget) : query.OrderBy(x => x.Budget),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }
}
