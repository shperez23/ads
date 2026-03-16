using AdsManager.Domain.Common;
using AdsManager.Domain.Enums;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class User : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;

    public Tenant Tenant { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
