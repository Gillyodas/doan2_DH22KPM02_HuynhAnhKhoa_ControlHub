using ControlHub.Domain.TokenManagement.Aggregates;

namespace ControlHub.Application.TokenManagement.Interfaces.Repositories
{
    public interface ITokenQueries
    {
        public Task<Token?> GetByValueAsync(string Value, CancellationToken cancellationToken);
    }
}
