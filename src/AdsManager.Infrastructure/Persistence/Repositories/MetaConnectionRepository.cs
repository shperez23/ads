using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Repositories;
using AdsManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdsManager.Infrastructure.Persistence.Repositories;

public sealed class MetaConnectionRepository : IMetaConnectionRepository
{
    private readonly IApplicationDbContext _dbContext;

    public MetaConnectionRepository(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<MetaConnection>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbContext.MetaConnections.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<MetaConnection?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
        => _dbContext.MetaConnections.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, cancellationToken);

    public async Task AddAsync(MetaConnection connection, CancellationToken cancellationToken = default)
    {
        _dbContext.MetaConnections.Add(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MetaConnection connection, CancellationToken cancellationToken = default)
    {
        _dbContext.MetaConnections.Update(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MetaConnection connection, CancellationToken cancellationToken = default)
    {
        _dbContext.MetaConnections.Remove(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
