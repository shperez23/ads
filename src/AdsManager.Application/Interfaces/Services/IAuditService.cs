namespace AdsManager.Application.Interfaces.Services;

public interface IAuditService
{
    Task LogAsync(
        Guid? userId,
        Guid tenantId,
        string action,
        string entityName,
        string entityId,
        string payloadJson,
        CancellationToken cancellationToken = default);
}

