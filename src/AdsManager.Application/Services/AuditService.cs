using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using AdsManager.Domain.Entities;

namespace AdsManager.Application.Services;

public sealed class AuditService : IAuditService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;

    public AuditService(IApplicationDbContext dbContext, ITenantProvider tenantProvider)
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
    }

    public async Task LogAsync(
        Guid? userId,
        Guid tenantId,
        string action,
        string entityName,
        string entityId,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            TenantId = tenantId,
            UserId = userId ?? Guid.Empty,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            PayloadJson = payloadJson,
            TraceId = _tenantProvider.GetTraceId()
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

