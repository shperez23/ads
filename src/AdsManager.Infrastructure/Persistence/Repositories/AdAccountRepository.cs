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
}
