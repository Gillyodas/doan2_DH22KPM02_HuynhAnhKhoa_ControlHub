using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace ControlHub.Infrastructure.Permissions.Repositories;

public class CachedPermissionRepository : IPermissionRepository
{
    private readonly IPermissionRepository _decorated;
    private readonly IMemoryCache _memoryCache;

    public CachedPermissionRepository(IPermissionRepository decorated, IMemoryCache memoryCache)
    {
        _decorated = decorated;
        _memoryCache = memoryCache;
    }

    public async Task AddAsync(Permission permission, CancellationToken cancellationToken)
    {
        await _decorated.AddAsync(permission, cancellationToken);
        // Invalidate specific cache entries if necessary, or just rely on expiry for lists
        // If we cached "AllPermissions", we would remove it here.
    }

    public async Task AddRangeAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken)
    {
        await _decorated.AddRangeAsync(permissions, cancellationToken);
    }

    public async Task DeleteAsync(Permission permission, CancellationToken cancellationToken)
    {
        await _decorated.DeleteAsync(permission, cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<Permission> permissions, CancellationToken cancellationToken)
    {
        await _decorated.DeleteRangeAsync(permissions, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken)
    {
        // For individual item caching, we could loop and checking cache, but GetByIds usually implies a batch fetch.
        // It's often better to cache the specific "Set" if it's a common query, or don't cache this specific batch method 
        // unless we break it down.
        // However, if we want to cache individual permissions:

        var permissions = new List<Permission>();
        var missingIds = new List<Guid>();

        foreach (var id in permissionIds)
        {
            string key = $"permission-{id}";
            if (_memoryCache.TryGetValue(key, out Permission? cachedPerm) && cachedPerm != null)
            {
                permissions.Add(cachedPerm);
            }
            else
            {
                missingIds.Add(id);
            }
        }

        if (missingIds.Count > 0)
        {
            var missingPermissions = await _decorated.GetByIdsAsync(missingIds, cancellationToken);
            foreach (var perm in missingPermissions)
            {
                string key = $"permission-{perm.Id}";
                _memoryCache.Set(key, perm, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                    SlidingExpiration = TimeSpan.FromMinutes(20)
                });
                permissions.Add(perm);
            }
        }

        return permissions;
    }

    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        string key = $"permission-{id}";
        if (_memoryCache.TryGetValue(key, out Permission? cachedPerm) && cachedPerm != null)
        {
            return cachedPerm;
        }

        var permission = await _decorated.GetByIdAsync(id, cancellationToken);
        if (permission != null)
        {
            _memoryCache.Set(key, permission, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(20)
            });
        }

        return permission;
    }
}
