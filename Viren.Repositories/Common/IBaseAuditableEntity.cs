namespace Viren.Repositories.Common;

public interface IBaseAuditableEntity<TId> : IBaseEntity<TId>
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    DateTimeOffset DeletedAt { get; set; }
}