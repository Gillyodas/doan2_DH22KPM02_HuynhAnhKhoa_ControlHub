using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Domain.Permissions;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Permissions.Repositories
{
    public class PermissionQueries : IPermissionQueries
    {
        private readonly AppDbContext _db;

        public PermissionQueries(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var entity = await _db.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            return entity is null ? null : PermissionMapper.ToDomain(entity);
        }

        public async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken)
        {
            var entities = await _db.Permissions
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return entities.Select(PermissionMapper.ToDomain).ToList();
        }

        public async Task<IEnumerable<Permission>> SearchByCodeAsync(string code, CancellationToken cancellationToken)
        {
            var entities = await _db.Permissions
                .AsNoTracking()
                .Where(p => p.Code.Contains(code))
                .ToListAsync(cancellationToken);

            return entities.Select(PermissionMapper.ToDomain).ToList();
        }

        public async Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Permissions
                .AsNoTracking()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync(cancellationToken)
                != null ? true : false;
        }

        public async Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
        {
            var entities = await _db.Permissions
            .AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

            return entities.Select(PermissionMapper.ToDomain).ToList();
        }
    }
}