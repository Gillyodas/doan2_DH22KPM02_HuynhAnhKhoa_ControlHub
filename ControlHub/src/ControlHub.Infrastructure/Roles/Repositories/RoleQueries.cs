using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Utils;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Roles.Repositories
{
    internal class RoleQueries : IRoleQueries
    {
        private readonly AppDbContext _db;

        public RoleQueries(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Roles
                .AsNoTrackingWithIdentityResolution()
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _db.Roles
                .AsNoTrackingWithIdentityResolution()
                .Include(r => r.Permissions)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Role>> SearchByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await _db.Roles
                .AsNoTrackingWithIdentityResolution()
                .Where(r => EF.Functions.Like(r.Name, $"%{name}%"))
                .Include(r => r.Permissions)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return await _db.Roles
                .AsNoTracking()
                .AnyAsync(r => r.Id == roleId, cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetPermissionIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return await _db.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken);
        }

        public async Task<PagedResult<Role>> SearchPaginationAsync(int pageIndex, int pageSize, string[] conditions, CancellationToken cancellationToken)
        {
            var query = _db.Roles.AsNoTracking();

            if (conditions != null && conditions.Length > 0)
            {
                // 1. Kh?i t?o Predicate là False (Ði?m m?u ch?t c?a logic OR)
                var predicate = PredicateBuilder.False<Role>();

                foreach (var rawTerm in conditions)
                {
                    if (string.IsNullOrWhiteSpace(rawTerm)) continue;
                    var term = rawTerm.Trim();

                    // 2. N?i thêm di?u ki?n b?ng hàm Or
                    // Logic: (Name ch?a term) HO?C (Description ch?a term)
                    predicate = predicate.Or(r => r.Name.Contains(term) || r.Description.Contains(term));
                }

                // 3. Ðua Predicate dã xây d?ng vào Query
                // SQL sinh ra: WHERE (Name LIKE %A% OR Desc LIKE %A%) OR (Name LIKE %B% OR Desc LIKE %B%)
                query = query.Where(predicate);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(r => r.Name)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Include(r => r.Permissions)
                .ToListAsync(cancellationToken);

            return new PagedResult<Role>(items, totalCount, pageIndex, pageSize);
        }
        public async Task<List<Application.Roles.DTOs.RoleDto>> GetRolesByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var query = from u in _db.Users.AsNoTracking()
                        join a in _db.Accounts.AsNoTracking() on u.AccId equals a.Id
                        join r in _db.Roles.AsNoTracking() on a.RoleId equals r.Id
                        where u.Id == userId
                        select new Application.Roles.DTOs.RoleDto(r.Id, r.Name, r.Description);

            return await query.ToListAsync(cancellationToken);
        }
    }
}
