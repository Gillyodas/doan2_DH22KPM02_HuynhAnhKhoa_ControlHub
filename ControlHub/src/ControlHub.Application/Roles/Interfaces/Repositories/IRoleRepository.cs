using ControlHub.Domain.Roles;

namespace ControlHub.Application.Roles.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid roleId, CancellationToken ct);
        Task AddAsync(Role role, CancellationToken ct);
        Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken cancellationToken);
        Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken);
        void Delete(Role role);
    }
}
