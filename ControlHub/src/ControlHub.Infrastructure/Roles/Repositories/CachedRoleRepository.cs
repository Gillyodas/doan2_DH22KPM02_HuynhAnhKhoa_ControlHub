using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.Roles;
using Microsoft.Extensions.Caching.Memory;

namespace ControlHub.Infrastructure.Roles.Repositories
{
    public class CachedRoleRepository : IRoleRepository
    {
        private readonly IRoleRepository _decorated;
        private readonly IMemoryCache _memoryCache;

        public CachedRoleRepository(IRoleRepository decorated, IMemoryCache memoryCache)
        {
            _decorated = decorated;
            _memoryCache = memoryCache;
        }

        public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken ct)
        {
            string cacheKey = $"role-{roleId}";

            return await _memoryCache.GetOrCreateAsync(
                cacheKey,
                entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

                    entry.SlidingExpiration = TimeSpan.FromMinutes(5);

                    return _decorated.GetByIdAsync(roleId, ct);
                });
        }

        public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await _decorated.GetByNameAsync(name, cancellationToken);
        }

        public async Task AddAsync(Role role, CancellationToken ct)
        {
            await _decorated.AddAsync(role, ct);
        }

        public async Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken cancellationToken)
        {
            await _decorated.AddRangeAsync(roles, cancellationToken);
        }
        public void Delete(Role role)
        {
            _decorated.Delete(role);
            _memoryCache.Remove($"role-{role.Id}");
        }
    }
}
