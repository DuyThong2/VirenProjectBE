using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Viren.Repositories;
using Viren.Repositories.Common;
using Viren.Repositories.Interfaces;

namespace Viren.Repositories.Impl;
public class GenericRepository<T, TId> : IGenericRepository<T, TId>
    where T : BaseEntity<TId>
    where TId : notnull
{
    private readonly AppDbContext _dbContext;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<T>();
    }

    public virtual IQueryable<T> Query(bool asNoTracking = true)
    {
        return asNoTracking
            ? _dbSet.AsNoTracking()
            : _dbSet;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(
    bool asNoTracking = true,
    CancellationToken cancellationToken = default
)
    {
        return asNoTracking
            ? await _dbSet.AsNoTracking().ToListAsync(cancellationToken)
            : await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        IEnumerable<Expression<Func<T, bool>>>? filters,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<T> query = _dbSet;

        if (filters != null)
        {
            foreach (var predicate in filters)
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(filters), "One of the filter expressions is null.");
                query = query.Where(predicate);
            }
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return asNoTracking
            ? await query.AsNoTracking().ToListAsync(cancellationToken)
            : await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default
    )
    {
        if (id == null) throw new ArgumentNullException(nameof(id));
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(
        int skip,
        int take,
        IEnumerable<Expression<Func<T, bool>>>? filters = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        IQueryable<T> query = _dbSet;

        // 1) Apply filters (AND’ed):
        if (filters != null)
        {
            foreach (var predicate in filters)
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(filters), "One of the filter expressions is null.");
                query = query.Where(predicate);
            }
        }

        // 2) Apply ordering (multi‐level) if provided:
        if (orderBy != null)
        {
            query = orderBy(query);
        }

        // 3) Skip/Take for paging:
        query = query.Skip(skip).Take(take);

        // 4) Materialize
        return asNoTracking
            ? await query.AsNoTracking().ToListAsync(cancellationToken)
            : await query.ToListAsync(cancellationToken);
    }

    private async Task<int> CountAsync(
        IEnumerable<Expression<Func<T, bool>>>? filters = null,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<T> query = _dbSet;

        if (filters != null)
        {
            foreach (var predicate in filters)
            {
                if (predicate == null)
                    throw new ArgumentNullException(nameof(filters), "One of the filter expressions is null.");
                query = query.Where(predicate);
            }
        }

        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<int> GetTotalPagesAsync(
        int pageSize,
        IEnumerable<Expression<Func<T, bool>>>? filters = null,
        CancellationToken cancellationToken = default
    )
    {
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be greater than zero.");

        // Reuse CountAsync(...) to apply all filters
        var totalCount = await CountAsync(filters, cancellationToken);

        // Compute ceil(totalCount / pageSize)
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }



    public virtual Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Add(entity);
        return Task.CompletedTask;
    }

    public virtual Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(entities);
        _dbSet.AddRange(entities);
        return Task.CompletedTask;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Attach(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public virtual Task RemoveAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        // entity.Status = false;
        _dbSet.Attach(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }
    public Task HardRemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }
    public virtual async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

}