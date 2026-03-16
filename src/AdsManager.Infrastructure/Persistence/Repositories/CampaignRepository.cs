using AdsManager.Application.DTOs.Campaigns;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class CampaignRepository : ICampaignRepository
{
    private readonly IApplicationDbContext _dbContext;

    public CampaignRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Campaign>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.Campaigns.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyCollection<Campaign> Items, int Total)> GetPagedByTenantAsync(Guid tenantId, CampaignListRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Campaigns.AsNoTracking().Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.MetaCampaignId.Contains(search) || x.Objective.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Status == request.Status);

        if (request.AdAccountId.HasValue)
            query = query.Where(x => x.AdAccountId == request.AdAccountId.Value);

        query = ApplySorting(query, request.SortBy, request.SortDirection);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.NormalizedPage - 1) * request.NormalizedPageSize)
            .Take(request.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Campaign?> GetByIdAsync(Guid tenantId, Guid campaignId, CancellationToken cancellationToken = default)
        => _dbContext.Campaigns.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == campaignId, cancellationToken);

    public Task<Campaign?> GetByMetaCampaignIdAsync(Guid tenantId, string metaCampaignId, CancellationToken cancellationToken = default)
        => _dbContext.Campaigns.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaCampaignId == metaCampaignId, cancellationToken);

    public async Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        _dbContext.Campaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        _dbContext.Campaigns.Update(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Campaign> ApplySorting(IQueryable<Campaign> query, string? sortBy, SortDirection sortDirection)
    {
        var desc = sortDirection == SortDirection.Desc;

        return sortBy?.ToLowerInvariant() switch
        {
            "name" => desc ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "objective" => desc ? query.OrderByDescending(x => x.Objective) : query.OrderBy(x => x.Objective),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }
}
