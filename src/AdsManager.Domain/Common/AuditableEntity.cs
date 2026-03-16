namespace AdsManager.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime? DeletedAt { get; set; }
}
