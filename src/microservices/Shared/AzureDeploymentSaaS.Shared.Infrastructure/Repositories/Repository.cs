using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AzureDeploymentSaaS.Shared.Infrastructure.Data;

namespace AzureDeploymentSaaS.Shared.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation with multi-tenant support
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly SaasDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(SaasDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, Guid? tenantId = null)
    {
        if (tenantId.HasValue && HasTenantIdProperty<T>())
        {
            return await _dbSet.FindAsync(id, tenantId.Value);
        }
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(Guid? tenantId = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (tenantId.HasValue && HasTenantIdProperty<T>())
        {
            query = ApplyTenantFilter(query, tenantId.Value);
        }
        
        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Guid? tenantId = null)
    {
        var query = _dbSet.Where(predicate);
        
        if (tenantId.HasValue && HasTenantIdProperty<T>())
        {
            query = ApplyTenantFilter(query, tenantId.Value);
        }
        
        return await query.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        var entry = await _dbSet.AddAsync(entity);
        return entry.Entity;
    }

    public virtual Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual async Task DeleteAsync(Guid id, Guid? tenantId = null)
    {
        var entity = await GetByIdAsync(id, tenantId);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task<bool> ExistsAsync(Guid id, Guid? tenantId = null)
    {
        var entity = await GetByIdAsync(id, tenantId);
        return entity != null;
    }

    public virtual async Task<int> CountAsync(Guid? tenantId = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (tenantId.HasValue && HasTenantIdProperty<T>())
        {
            query = ApplyTenantFilter(query, tenantId.Value);
        }
        
        return await query.CountAsync();
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Guid? tenantId = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (tenantId.HasValue && HasTenantIdProperty<T>())
        {
            query = ApplyTenantFilter(query, tenantId.Value);
        }
        
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    private static bool HasTenantIdProperty<TEntity>()
    {
        return typeof(TEntity).GetProperty("TenantId") != null;
    }

    private static IQueryable<TEntity> ApplyTenantFilter<TEntity>(IQueryable<TEntity> query, Guid tenantId) where TEntity : class
    {
        if (!HasTenantIdProperty<TEntity>())
            return query;

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var property = Expression.Property(parameter, "TenantId");
        var constant = Expression.Constant(tenantId);
        var equality = Expression.Equal(property, constant);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(equality, parameter);

        return query.Where(lambda);
    }
}

/// <summary>
/// Unit of work implementation for transaction management
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SaasDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();
    private bool _disposed = false;

    public UnitOfWork(SaasDbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (_repositories.ContainsKey(type))
        {
            return (IRepository<T>)_repositories[type];
        }

        var repository = new Repository<T>(_context);
        _repositories.Add(type, repository);
        return repository;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.CommitAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.RollbackAsync();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }
}