using System.Linq.Expressions;

namespace AzureDeploymentSaaS.Shared.Infrastructure.Repositories;

/// <summary>
/// Generic repository interface for data access
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, Guid? tenantId = null);
    Task<IEnumerable<T>> GetAllAsync(Guid? tenantId = null);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, Guid? tenantId = null);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id, Guid? tenantId = null);
    Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
    Task<int> CountAsync(Guid? tenantId = null);
    Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, Guid? tenantId = null);
}

/// <summary>
/// Unit of work interface for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}