namespace ControlHub.Application.Common.Repositories
{
    public interface ICommandRepositoryBase<T> where T : class
    {
        Task AddAsync(T entity, CancellationToken cancellationToken);
        Task UpdateAsync(T entity, CancellationToken cancellationToken);
        Task DeleteAsync(T entity, CancellationToken cancellationToken);
    }
}
