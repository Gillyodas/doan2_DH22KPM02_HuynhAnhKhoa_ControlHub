using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Identity.Identifiers
{
    public interface IIdentifierConfigRepository
    {
        Task<Result<IdentifierConfig>> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Result<IdentifierConfig>> GetByNameAsync(string name, CancellationToken ct);
        Task<Result<IEnumerable<IdentifierConfig>>> GetActiveConfigsAsync(CancellationToken ct);
        Task<Result<IEnumerable<IdentifierConfig>>> GetDeactiveConfigsAsync(CancellationToken ct);
        Task<Result> AddAsync(IdentifierConfig config, CancellationToken ct);
    }
}
