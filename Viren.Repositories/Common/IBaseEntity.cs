namespace Viren.Repositories.Common;

public interface IBaseEntity<TId>
{
    TId Id { get; set; }
}