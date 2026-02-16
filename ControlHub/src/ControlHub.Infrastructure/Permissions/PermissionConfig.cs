using ControlHub.Domain.AccessControl.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Permissions
{
    internal class PermissionConfig : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Code)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(p => p.Code)
                .IsUnique();

            builder.Property(p => p.Description)
                .HasMaxLength(255);
        }
    }
}
