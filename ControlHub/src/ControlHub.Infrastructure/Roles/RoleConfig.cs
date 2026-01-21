using ControlHub.Domain.Roles;
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

            // --- CẤU HÌNH MANY-TO-MANY ---

            builder.HasMany(r => r.Permissions) // Role có nhiều Permission
                .WithMany()                     // Permission (Domain) không cần biết về Role
                .UsingEntity<RolePermissionEntity>( // Sử dụng bảng trung gian này
                    join => join
                        .HasOne(rp => rp.Permission)
                        .WithMany()
                        .HasForeignKey(rp => rp.PermissionId),
                    join => join
                        .HasOne(rp => rp.Role)
                        .WithMany() // Nếu Role không có navigation property tới RolePermissionEntity thì để trống
                        .HasForeignKey(rp => rp.RoleId),
                    join =>
                    {
                        join.ToTable("RolePermissions"); // Tên bảng trong DB
                        join.HasKey(rp => new { rp.RoleId, rp.PermissionId }); // Khóa chính phức hợp
                    }
                );

            // --- CẤU HÌNH FIELD ACCESS MODE ---
            // Bảo EF Core đọc/ghi trực tiếp vào field "_permissions"
            // thay vì property "Permissions" (vì property chỉ là ReadOnly)
            builder.Navigation(r => r.Permissions)
                .HasField("_permissions")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}