namespace ControlHub.Domain.Roles
{
    public class RolePermission
    {
        public Guid RoleId { get; private set; }
        public Guid PermissionId { get; private set; }

        // Constructor mặc định cho EF Core
        private RolePermission() { }

        public RolePermission(Guid roleId, Guid permissionId)
        {
            RoleId = roleId;
            PermissionId = permissionId;
        }
    }
}