namespace Viren.Repositories.Common;

public class BaseEntity<TId> : IBaseEntity
{
    public TId Id { get; set; } = default!;
    public bool Status { get; set; }
}