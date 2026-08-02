using URMS.Domain.Contracts;

namespace URMS.Application.Contracts.Persistence;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;
    Task<int> CompleteAsync();
}
