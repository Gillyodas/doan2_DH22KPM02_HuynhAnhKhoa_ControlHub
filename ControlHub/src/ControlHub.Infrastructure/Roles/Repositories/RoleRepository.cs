using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Roles.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _db;

        public RoleRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return await _db.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        }

        public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await _db.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        }

        public async Task AddAsync(Role role, CancellationToken cancellationToken)
        {
            await _db.Roles.AddAsync(role, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken cancellationToken)
        {
            await _db.Roles.AddRangeAsync(roles, cancellationToken);
        }

        public void Delete(Role role)
        {
            _db.Roles.Remove(role);
        }
    }
}
