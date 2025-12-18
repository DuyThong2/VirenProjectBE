using Viren.Repositories.Common;
using System.Linq.Expressions;

namespace Viren.Repositories.Interfaces;


public interface IGenericRepository<T, TId> where T : BaseEntity<TId>
{
    IQueryable<T> Query(bool asNoTracking = true);

    Task<IEnumerable<T>> GetAllAsync(
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<T>> FindAsync(
        IEnumerable<Expression<Func<T, bool>>>? filters,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    );

    Task<T?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<T>> GetPagedAsync(
        int skip,
        int take,
        IEnumerable<Expression<Func<T, bool>>>? filters = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    );

    Task<int> GetTotalPagesAsync(
        int pageSize,
        IEnumerable<Expression<Func<T, bool>>>? filters = null,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default
    );

    Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default
    );

    Task UpdateAsync(
        T entity,
        CancellationToken cancellationToken = default
    );

    Task RemoveAsync(
        T entity,
        CancellationToken cancellationToken = default
    );
    Task HardRemoveRangeAsync
    (IEnumerable<T> entities, 
        CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    );
}