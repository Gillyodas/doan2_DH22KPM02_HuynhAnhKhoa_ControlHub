using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.Domain.AccessControl.Entities;

namespace ControlHub.Infrastructure.AccessControl.Persistence.Repositories
{
    // Class n�y thu?n t�y l� POCO cho b?ng trung gian (Join Table)
    public class RolePermissionEntity
    {
        public Guid RoleId { get; set; }
        public Guid PermissionId { get; set; }

        public Role Role { get; set; } = default!;
        public Permission Permission { get; set; } = default!;
    }
}
