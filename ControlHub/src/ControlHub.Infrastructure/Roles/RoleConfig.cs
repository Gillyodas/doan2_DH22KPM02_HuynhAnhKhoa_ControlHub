using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.Infrastructure.RolePermissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Roles
{
    internal class RoleConfig : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(r => r.Description)
                .HasMaxLength(255);

            builder.Property(r => r.IsActive)
                .IsRequired();

            builder.Property(r => r.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasQueryFilter(r => !r.IsDeleted);

            // --- C?U HÌNH MANY-TO-MANY ---

            builder.HasMany(r => r.Permissions) // Role có nhi?u Permission
                .WithMany()                     // Permission (Domain) không c?n bi?t v? Role
                .UsingEntity<RolePermissionEntity>( // S? d?ng b?ng trung gian này
                    join => join
                        .HasOne(rp => rp.Permission)
                        .WithMany()
                        .HasForeignKey(rp => rp.PermissionId),
                    join => join
                        .HasOne(rp => rp.Role)
                        .WithMany() // N?u Role không có navigation property t?i RolePermissionEntity thì d? tr?ng
                        .HasForeignKey(rp => rp.RoleId),
                    join =>
                    {
                        join.ToTable("RolePermissions"); // Tên b?ng trong DB
                        join.HasKey(rp => new { rp.RoleId, rp.PermissionId }); // Khóa chính ph?c h?p
                    }
                );

            // --- C?U HÌNH FIELD ACCESS MODE ---
            // B?o EF Core d?c/ghi tr?c ti?p vào field "_permissions"
            // thay vì property "Permissions" (vì property ch? là ReadOnly)
            builder.Navigation(r => r.Permissions)
                .HasField("_permissions")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
