using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Domain.Permissions;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Utils;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Permissions.Repositories
{
    internal class PermissionQueries : IPermissionQueries
    {
        private readonly AppDbContext _db;

        public PermissionQueries(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _db.Permissions
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Permission>> SearchByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return await _db.Permissions
                .AsNoTracking()
                .Where(p => p.Code.Contains(code))
                .ToListAsync(cancellationToken);
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
            return await _db.Permissions
                .AsNoTracking()
                .Where(p => permissionIds.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return await _db.Permissions
                .AsNoTracking()
                .Where(p => _db.RolePermissions
                    .Any(rp => rp.RoleId == roleId && rp.PermissionId == p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Permission>> SearchPaginationAsync(int pageIndex, int pageSize, string[] conditions, CancellationToken cancellationToken)
        {
            var query = _db.Permissions.AsNoTracking();

            if (conditions != null && conditions.Length > 0)
            {
                var predicate = PredicateBuilder.False<Permission>();

                foreach (var rawTerm in conditions)
                {
                    if (string.IsNullOrWhiteSpace(rawTerm)) continue;
                    var term = rawTerm.Trim();

                    predicate = predicate.Or(r => r.Code.Contains(term) || r.Description.Contains(term));
                }

                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(r => r.Code)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Permission>(items, totalCount, pageIndex, pageSize);
        }
    }
}