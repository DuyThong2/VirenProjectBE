using System.Collections;
using Viren.Repositories.Common;
using Viren.Repositories.Interfaces;


namespace Viren.Repositories.Impl;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly Hashtable _repos = new();

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T, TId> GetRepository<T, TId>()
        where T : class, IBaseEntity<TId>
        where TId : notnull
    {
        var typeName = typeof(T).Name;
        if (_repos.ContainsKey(typeName))
            return (IGenericRepository<T, TId>)_repos[typeName]!;

        var repoInstance = new GenericRepository<T, TId>(_context);
        _repos.Add(typeName, repoInstance);
        return repoInstance;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);

    //public void Dispose() =>
        //_context.Dispose();
}