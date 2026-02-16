using ControlHub.Domain.Tokens;

namespace ControlHub.Application.Tokens.Interfaces.Repositories
{
    public interface ITokenQueries
    {
        public Task<Token?> GetByValueAsync(string Value, CancellationToken cancellationToken);
    }
}
