using System.Linq.Expressions;

namespace URMS.Domain.Contracts;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includeAction = null);
    Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includeAction = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null);

    /// <summary>
    /// Returns a paginated result with total count. Includes, filtering, and ordering are handled via callback delegates.
    /// </summary>
    Task<(IEnumerable<T> Items, int TotalCount)> FindPagedAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? includeAction = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null);

    /// <summary>
    /// Returns the count of entities matching the predicate.
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
