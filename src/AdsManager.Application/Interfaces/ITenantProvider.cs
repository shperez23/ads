namespace AdsManager.Application.Interfaces;

public interface ITenantProvider
{
    Guid? GetTenantId();
    Guid? GetUserId();
    string GetTraceId();
    string GetClientIp();
}
