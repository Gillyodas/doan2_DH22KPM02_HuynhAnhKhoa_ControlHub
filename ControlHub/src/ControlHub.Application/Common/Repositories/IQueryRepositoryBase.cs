namespace ControlHub.Application.Common.Repositories
{
    public interface IQueryRepositoryBase<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
