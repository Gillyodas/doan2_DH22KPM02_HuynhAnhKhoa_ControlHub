using ControlHub.Domain.Tokens;

namespace ControlHub.Application.Tokens.Interfaces.Repositories
{
    public interface ITokenCommands
    {
        Task AddAsync(Token domainToken, CancellationToken cancellationToken);
        Task UpdateAsync(Token domainToken, CancellationToken cancellationToken);
    }
}
