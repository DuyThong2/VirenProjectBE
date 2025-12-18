using Viren.Repositories.Common;

namespace Viren.Repositories.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<T, TId> GetRepository<T, TId>()
        where T : BaseEntity<TId>
        where TId : notnull;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}