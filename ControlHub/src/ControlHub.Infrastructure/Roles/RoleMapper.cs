using ControlHub.Domain.Permissions;
using ControlHub.Domain.Roles;
using ControlHub.Infrastructure.RolePermissions;

namespace ControlHub.Infrastructure.Roles
{
    public static class RoleMapper
    {
        public static Role ToDomain(RoleEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var permissions = entity.RolePermissions
                .Where(rp => rp.Permission != null)
                .Select(rp => Permission.Rehydrate(
                    rp.Permission.Id,
                    rp.Permission.Code,
                    rp.Permission.Description
                ))
                .ToList();

            return Role.Rehydrate(
                entity.Id,
                entity.Name,
                entity.Description,
                entity.IsActive,
                permissions
            );
        }

        public static RoleEntity ToEntity(Role domain)
        {
            if (domain == null)
                throw new ArgumentNullException(nameof(domain));

            var roleEntity = new RoleEntity
            {
                Id = domain.Id,
                Name = domain.Name,
                Description = domain.Description,
                IsActive = domain.IsActive,
                RolePermissions = domain.Permissions.Select(p => new RolePermissionEntity
                {
                    RoleId = domain.Id,
                    PermissionId = p.Id // <-- chỉ gán ID thôi, không khởi tạo PermissionEntity
                }).ToList()
            };

            return roleEntity;
        }
    }
}