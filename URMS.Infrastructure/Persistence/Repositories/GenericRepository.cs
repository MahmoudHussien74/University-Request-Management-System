using System.Linq.Expressions;
using URMS.Domain.Contracts;
namespace URMS.Infrastructure.Persistence.Repositories;
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public async Task<T?> GetByIdAsync(string id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await _dbSet.Where(predicate).ToListAsync();

    public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includeAction = null)
    {
        IQueryable<T> query = _dbSet;

        if (includeAction != null)
        {
            query = includeAction(query);
        }

        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<T>> FindAllAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IQueryable<T>>? includeAction = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        IQueryable<T> query = _dbSet.Where(predicate);

        if (includeAction != null)
        {
            query = includeAction(query);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.ToListAsync();
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> FindPagedAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IQueryable<T>>? includeAction = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        IQueryable<T> query = _dbSet;

        if (includeAction != null)
            query = includeAction(query);

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        var totalCount = await query.CountAsync();

        if (pageSize.HasValue && pageSize > 0)
        {
            var pNum = pageNumber.HasValue && pageNumber > 0 ? pageNumber.Value : 1;
            query = query.Skip((pNum - 1) * pageSize.Value).Take(pageSize.Value);
        }

        var items = await query.ToListAsync();
        return (items, totalCount);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate != null
            ? await _dbSet.CountAsync(predicate)
            : await _dbSet.CountAsync();
    }

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    /// <summary>
    /// Protected: available to specialized repositories in Infrastructure only.
    /// NOT exposed via IGenericRepository interface.
    /// </summary>
    protected IQueryable<T> GetQueryable() => _dbSet;

    public void Update(T entity) => _dbSet.Update(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);
}