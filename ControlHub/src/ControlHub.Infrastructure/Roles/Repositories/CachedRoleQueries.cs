using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.SharedKernel.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Roles.Repositories
{
    /// <summary>
    /// Decorator pattern để thêm caching layer cho RoleQueries (read operations).
    /// Cache strategy:
    /// - GetByIdAsync: Key = "role-{id}" (30min TTL, 5min sliding)
    /// - GetAllAsync: Key = "roles-all" (30min TTL, 5min sliding)
    /// - SearchByNameAsync: Key = "roles-search-{name}" (10min TTL)
    /// - GetPermissionIdsByRoleIdAsync: Key = "role-perms-{roleId}" (30min TTL, 5min sliding)
    /// - Paginated queries: Không cache (thường có filters động)
    /// </summary>
    public class CachedRoleQueries : IRoleQueries
    {
        private readonly IRoleQueries _decorated;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CachedRoleQueries> _logger;

        // Cache configuration
        private const int AbsoluteExpirationMinutes = 30;
        private const int SlidingExpirationMinutes = 5;
        private const int SearchCacheMinutes = 10;

        public CachedRoleQueries(
            IRoleQueries decorated,
            IMemoryCache memoryCache,
            ILogger<CachedRoleQueries> logger)
        {
            _decorated = decorated;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        /// <summary>
        /// Lấy role theo ID với caching.
        /// Cache hit: trả về từ memory (rất nhanh)
        /// Cache miss: query DB, lưu vào cache
        /// </summary>
        public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            string cacheKey = $"role-{id}";

            if (_memoryCache.TryGetValue(cacheKey, out Role? cachedRole))
            {
                _logger.LogInformation(
                    ">>> CACHE HIT: Role {RoleId} found in memory",
                    id);
                return cachedRole;
            }

            _logger.LogWarning(
                ">>> CACHE MISS: Role {RoleId} not found. Fetching from database...",
                id);

            var role = await _decorated.GetByIdAsync(id, cancellationToken);

            if (role != null)
            {
                SetRoleCache(cacheKey, role);
            }

            return role;
        }

        /// <summary>
        /// Lấy tất cả roles với caching.
        /// Được cache để tránh N+1 queries khi load full list.
        /// </summary>
        public async Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken)
        {
            const string cacheKey = "roles-all";

            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<Role>? cachedRoles))
            {
                _logger.LogInformation(">>> CACHE HIT: All roles found in memory");
                return cachedRoles!;
            }

            _logger.LogWarning(
                ">>> CACHE MISS: All roles not found. Fetching from database...");

            var roles = await _decorated.GetAllAsync(cancellationToken);
            var roleList = roles.ToList();

            if (roleList.Any())
            {
                _memoryCache.Set(
                    cacheKey,
                    (IEnumerable<Role>)roleList,
                    GetCacheOptions());

                _logger.LogInformation(
                    ">>> CACHE SET: Cached {Count} roles",
                    roleList.Count);
            }

            return roleList;
        }

        /// <summary>
        /// Tìm kiếm roles theo name.
        /// Cached với TTL ngắn hơn (10 phút) vì search term có thể đa dạng.
        /// </summary>
        public async Task<IEnumerable<Role>> SearchByNameAsync(
            string name,
            CancellationToken cancellationToken)
        {
            string cacheKey = $"roles-search-{name.ToLowerInvariant()}";

            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<Role>? cachedSearchResults))
            {
                _logger.LogInformation(
                    ">>> CACHE HIT: Search results for '{SearchTerm}' found in memory",
                    name);
                return cachedSearchResults!;
            }

            _logger.LogWarning(
                ">>> CACHE MISS: Search results for '{SearchTerm}' not found. Fetching from database...",
                name);

            var roles = await _decorated.SearchByNameAsync(name, cancellationToken);
            var roleList = roles.ToList();

            if (roleList.Any())
            {
                _memoryCache.Set(
                    cacheKey,
                    (IEnumerable<Role>)roleList,
                    GetSearchCacheOptions());

                _logger.LogInformation(
                    ">>> CACHE SET: Cached {Count} search results for '{SearchTerm}'",
                    roleList.Count,
                    name);
            }

            return roleList;
        }

        /// <summary>
        /// Kiểm tra role có tồn tại không.
        /// Không cache vì query cực nhanh (chỉ COUNT) và cần real-time accuracy.
        /// </summary>
        public async Task<bool> ExistAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return await _decorated.ExistAsync(roleId, cancellationToken);
        }

        /// <summary>
        /// Lấy danh sách Permission IDs của một role.
        /// Cache vì thường được gọi nhiều lần cho cùng role.
        /// Key: "role-perms-{roleId}"
        /// </summary>
        public async Task<IReadOnlyList<Guid>> GetPermissionIdsByRoleIdAsync(
            Guid roleId,
            CancellationToken cancellationToken)
        {
            string cacheKey = $"role-perms-{roleId}";

            if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<Guid>? cachedPermissionIds))
            {
                _logger.LogInformation(
                    ">>> CACHE HIT: Permission IDs for Role {RoleId} found in memory",
                    roleId);
                return cachedPermissionIds!;
            }

            _logger.LogWarning(
                ">>> CACHE MISS: Permission IDs for Role {RoleId} not found. Fetching from database...",
                roleId);

            var permissionIds = await _decorated.GetPermissionIdsByRoleIdAsync(roleId, cancellationToken);

            if (permissionIds.Count > 0)
            {
                _memoryCache.Set(
                    cacheKey,
                    permissionIds,
                    GetCacheOptions());

                _logger.LogInformation(
                    ">>> CACHE SET: Cached {Count} permission IDs for Role {RoleId}",
                    permissionIds.Count,
                    roleId);
            }

            return permissionIds;
        }

        /// <summary>
        /// Tìm kiếm roles với phân trang.
        /// Không cache vì:
        /// 1. Filters động (pageIndex, pageSize, conditions)
        /// 2. Số lượng kết hợp filters có thể rất lớn
        /// 3. Data thường thay đổi, cache invalidation phức tạp
        /// </summary>
        public async Task<PagedResult<Role>> SearchPaginationAsync(
            int pageIndex,
            int pageSize,
            string[] conditions,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug(
                "Paginated search (no cache): pageIndex={PageIndex}, pageSize={PageSize}, conditions={ConditionCount}",
                pageIndex,
                pageSize,
                conditions?.Length ?? 0);

            return await _decorated.SearchPaginationAsync(
                pageIndex,
                pageSize,
                conditions,
                cancellationToken);
        }

        /// <summary>
        /// Lấy roles của một user.
        /// Không cache vì:
        /// 1. Data user-specific (cache key sẽ là "user-roles-{userId}")
        /// 2. Có thể có nhiều users, dễ tràn bộ nhớ
        /// 3. Có thể implement riêng cache nếu cần (e.g., user-permission cache)
        /// </summary>
        public async Task<List<Application.Roles.DTOs.RoleDto>> GetRolesByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Fetching roles for user {UserId} (no cache)", userId);

            return await _decorated.GetRolesByUserIdAsync(userId, cancellationToken);
        }

        /// <summary>
        /// Xóa cache của một role theo ID.
        /// Được gọi từ DomainEventHandler khi role thay đổi.
        /// </summary>
        public void InvalidateRoleCache(Guid roleId)
        {
            string cacheKey = $"role-{roleId}";
            _memoryCache.Remove(cacheKey);
            _logger.LogInformation(">>> CACHE INVALIDATED: Role {RoleId}", roleId);
        }

        /// <summary>
        /// Xóa cache roles-all.
        /// Được gọi khi role được tạo/xóa (thay đổi list).
        /// </summary>
        public void InvalidateAllRolesCache()
        {
            _memoryCache.Remove("roles-all");
            _logger.LogInformation(">>> CACHE INVALIDATED: All roles cache cleared");
        }

        /// <summary>
        /// Xóa cache permission IDs của một role.
        /// Được gọi từ DomainEventHandler khi permissions thay đổi.
        /// </summary>
        public void InvalidatePermissionsCacheForRole(Guid roleId)
        {
            string cacheKey = $"role-perms-{roleId}";
            _memoryCache.Remove(cacheKey);
            _logger.LogInformation(
                ">>> CACHE INVALIDATED: Permission IDs for Role {RoleId}",
                roleId);
        }

        /// <summary>
        /// Xóa toàn bộ search cache.
        /// (Optional: nếu muốn invalidate tất cả search results)
        /// </summary>
        public void InvalidateAllSearchCache()
        {
            // Note: IMemoryCache không có cách native để delete all keys với pattern
            // Giải pháp: 
            // 1. Nếu số lượng search cache ít → Accept (sẽ expire sau TTL)
            // 2. Nếu cần invalidate immediately → Dùng custom cache wrapper
            _logger.LogWarning(
                ">>> CACHE PARTIAL INVALIDATION: Search cache cannot be cleared by pattern. "
                + "Will expire automatically after {Minutes} minutes",
                SearchCacheMinutes);
        }

        // ============ Private Helper Methods ============

        /// <summary>
        /// Set cache với default options (30 phút absolute, 5 phút sliding)
        /// </summary>
        private void SetRoleCache(string cacheKey, Role role)
        {
            _memoryCache.Set(cacheKey, role, GetCacheOptions());
            _logger.LogInformation(">>> CACHE SET: Role {RoleId}", role.Id);
        }

        /// <summary>
        /// Default cache options: 30 min absolute + 5 min sliding
        /// Sliding = automatic refresh nếu accessed
        /// </summary>
        private MemoryCacheEntryOptions GetCacheOptions()
        {
            return new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(AbsoluteExpirationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(SlidingExpirationMinutes)
            };
        }

        /// <summary>
        /// Search cache options: 10 min (shorter TTL)
        /// Vì search filters thường nhiều tổ hợp khác nhau
        /// </summary>
        private MemoryCacheEntryOptions GetSearchCacheOptions()
        {
            return new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(SearchCacheMinutes)
            };
        }
    }
}