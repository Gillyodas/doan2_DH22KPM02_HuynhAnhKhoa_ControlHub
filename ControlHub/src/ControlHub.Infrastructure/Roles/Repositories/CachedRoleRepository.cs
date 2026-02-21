using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Aggregates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Roles.Repositories
{
    public class CachedRoleRepository : IRoleRepository
    {
        private readonly IRoleRepository _decorated;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CachedRoleRepository> _logger;

        public CachedRoleRepository(
            IRoleRepository decorated, 
            IMemoryCache memoryCache,
            ILogger<CachedRoleRepository> logger)
        {
            _decorated = decorated;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<Role?> GetByIdAsync(Guid roleId, CancellationToken ct)
        {
            _logger.LogInformation("***************************************************GetByIdAsync");
            string cacheKey = $"role-{roleId}";

            if (_memoryCache.TryGetValue(cacheKey, out Role? cachedRole))
            {
                _logger.LogInformation(">>> CACHE HIT: Role {RoleId} found in memory.", roleId);
                return cachedRole;
            }

            _logger.LogWarning(">>> CACHE MISS: Role {RoleId} not found. Fetching from Database...", roleId);

            var role = await _decorated.GetByIdAsync(roleId, ct);

            if (role != null)
            {
                _memoryCache.Set(cacheKey, role, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            return role;
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
