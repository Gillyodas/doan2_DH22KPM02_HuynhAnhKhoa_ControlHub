using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.Infrastructure.AccessControl.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.AccessControl.Persistence.Configurations
{
    internal class RoleConfiguration : IEntityTypeConfiguration<Role>
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

            // --- C?U H�NH MANY-TO-MANY ---

            builder.HasMany(r => r.Permissions) // Role c� nhi?u Permission
                .WithMany()                     // Permission (Domain) kh�ng c?n bi?t v? Role
                .UsingEntity<RolePermissionEntity>( // S? d?ng b?ng trung gian n�y
                    join => join
                        .HasOne(rp => rp.Permission)
                        .WithMany()
                        .HasForeignKey(rp => rp.PermissionId),
                    join => join
                        .HasOne(rp => rp.Role)
                        .WithMany() // N?u Role kh�ng c� navigation property t?i RolePermissionEntity th� d? tr?ng
                        .HasForeignKey(rp => rp.RoleId),
                    join =>
                    {
                        join.ToTable("RolePermissions"); // T�n b?ng trong DB
                        join.HasKey(rp => new { rp.RoleId, rp.PermissionId }); // Kh�a ch�nh ph?c h?p
                    }
                );

            // --- C?U H�NH FIELD ACCESS MODE ---
            // B?o EF Core d?c/ghi tr?c ti?p v�o field "_permissions"
            // thay v� property "Permissions" (v� property ch? l� ReadOnly)
            builder.Navigation(r => r.Permissions)
                .HasField("_permissions")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
