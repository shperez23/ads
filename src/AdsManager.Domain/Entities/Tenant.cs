using AdsManager.Domain.Common;
using AdsManager.Domain.Enums;

namespace AdsManager.Domain.Entities;

public sealed class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public ICollection<User> Users { get; set; } = new List<User>();
}
