using AdsManager.Application.DTOs.Ads;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class AdRepository : IAdRepository
{
    private readonly IApplicationDbContext _dbContext;

    public AdRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Ad>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.Ads
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyCollection<Ad> Items, int Total)> GetPagedByTenantAsync(Guid tenantId, AdListRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Ads
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (request.CampaignId.HasValue)
        {
            query = query.Where(x => _dbContext.AdSets
                .Where(a => a.Id == x.AdSetId)
                .Any(a => a.CampaignId == request.CampaignId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.MetaAdId.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Status == request.Status);

        query = ApplySorting(query, request.SortBy, request.SortDirection);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.NormalizedPage - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Ad?> GetByIdAsync(Guid tenantId, Guid adId, CancellationToken cancellationToken = default)
        => _dbContext.Ads.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == adId, cancellationToken);

    public async Task AddAsync(Ad ad, CancellationToken cancellationToken = default)
    {
        _dbContext.Ads.Add(ad);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Ad ad, CancellationToken cancellationToken = default)
    {
        _dbContext.Ads.Update(ad);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Ad> ApplySorting(IQueryable<Ad> query, string? sortBy, SortDirection sortDirection)
    {
        var desc = sortDirection == SortDirection.Desc;

        return sortBy?.ToLowerInvariant() switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }
}
