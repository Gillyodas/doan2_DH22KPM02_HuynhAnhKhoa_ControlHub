using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.Roles;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Roles.Repositories
{
    public class RoleQueries : IRoleQueries
    {
        private readonly AppDbContext _db;

        public RoleQueries(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var entity = await _db.Roles
                .AsNoTracking()
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            return entity is not null ? RoleMapper.ToDomain(entity) : null;
        }

        public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken)
        {
            var entities = await _db.Roles
                .AsNoTracking()
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToListAsync(cancellationToken);

            return entities.Select(RoleMapper.ToDomain);
        }

        public async Task<IEnumerable<Role>> SearchByNameAsync(string name, CancellationToken cancellationToken)
        {
            var entities = await _db.Roles
                .AsNoTracking()
                .Where(r => EF.Functions.Like(r.Name, $"%{name}%"))
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToListAsync(cancellationToken);

            return entities.Select(RoleMapper.ToDomain);
        }

        public async Task<bool> ExistAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return await _db.Roles
                .AsNoTracking()
                .AnyAsync(r => r.Id == roleId, cancellationToken);
        }
    }
}