namespace Viren.Repositories.Common;

public abstract class BaseAuditableEntity<TId> : BaseEntity<TId>, IBaseAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset DeletedAt { get; set; }
}