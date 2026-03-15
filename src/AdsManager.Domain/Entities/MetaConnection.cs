using AdsManager.Domain.Common;
using AdsManager.Domain.Enums;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class MetaConnection : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime TokenExpiration { get; set; }
    public string BusinessId { get; set; } = string.Empty;
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Connected;
}
