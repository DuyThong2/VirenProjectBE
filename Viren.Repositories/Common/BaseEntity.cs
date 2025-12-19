namespace Viren.Repositories.Common;

public abstract class BaseEntity<TId> : IBaseEntity<TId>
{
    public TId Id { get; set; } = default!;
}
