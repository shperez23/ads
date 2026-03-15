using AdsManager.Domain.Common;
using AdsManager.Domain.Interfaces;

namespace AdsManager.Domain.Entities;

public sealed class Ad : AuditableEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid AdSetId { get; set; }
    public string MetaAdId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreativeJson { get; set; } = "{}";
    public string? PreviewUrl { get; set; }

    public AdSet AdSet { get; set; } = null!;
}
