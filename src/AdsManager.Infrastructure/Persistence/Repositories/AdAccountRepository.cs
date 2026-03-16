using AdsManager.Application.DTOs.AdAccounts;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class AdAccountRepository : IAdAccountRepository
{
    private readonly IApplicationDbContext _dbContext;

    public AdAccountRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<AdAccount>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.AdAccounts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyCollection<AdAccount> Items, int Total)> GetPagedByTenantAsync(Guid tenantId, AdAccountListRequest request, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AdAccounts.AsNoTracking().Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search) || x.MetaAccountId.Contains(search));
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

    public Task<AdAccount?> GetByIdAsync(Guid tenantId, Guid adAccountId, CancellationToken cancellationToken = default)
        => _dbContext.AdAccounts.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == adAccountId, cancellationToken);

    public Task<AdAccount?> GetByMetaAccountIdAsync(Guid tenantId, string metaAccountId, CancellationToken cancellationToken = default)
        => _dbContext.AdAccounts.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.MetaAccountId == metaAccountId, cancellationToken);

    public async Task AddAsync(AdAccount adAccount, CancellationToken cancellationToken = default)
    {
        _dbContext.AdAccounts.Add(adAccount);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AdAccount adAccount, CancellationToken cancellationToken = default)
    {
        _dbContext.AdAccounts.Update(adAccount);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<AdAccount> ApplySorting(IQueryable<AdAccount> query, string? sortBy, SortDirection sortDirection)
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
