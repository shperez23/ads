namespace AdsManager.Domain.Interfaces;

public interface ITenantScoped
{
    Guid TenantId { get; }
}
