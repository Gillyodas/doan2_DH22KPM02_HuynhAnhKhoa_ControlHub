// File: /Infrastructure/Accounts/IdentifierConfigConfig.cs
using ControlHub.Domain.Accounts.Identifiers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Accounts
{
    internal class IdentifierConfigConfig : IEntityTypeConfiguration<IdentifierConfig>
    {
        public void Configure(EntityTypeBuilder<IdentifierConfig> builder)
        {
            builder.ToTable("IdentifierConfigs", "ControlHub");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Description)
                .HasMaxLength(500);

            builder.Property(c => c.IsActive)
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt);

            // ===== OWNED COLLECTION: ValidationRules =====
            builder.OwnsMany(c => c.Rules, rb =>
            {
                // 1. Tên bảng
                rb.ToTable("IdentifierValidationRules", "ControlHub");

                // 2. Foreign Key (shadow property)
                rb.WithOwner().HasForeignKey("IdentifierConfigId");

                // 3. Primary Key (chỉ dùng Id của ValidationRule)
                rb.HasKey(r => r.Id);

                // 4. Properties mapping
                rb.Property(r => r.Type)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);

                rb.Property(r => r.ParametersJson)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                rb.Property(r => r.ErrorMessage)
                    .HasMaxLength(500);

                rb.Property(r => r.Order)
                    .IsRequired()
                    .HasDefaultValue(0);

                // 5. Index cho performance (sort by Order)
                rb.HasIndex(r => r.Order);

                // 6. Index cho ForeignKey
                rb.HasIndex("IdentifierConfigId");
            });

            // 7. Navigation property access mode
            builder.Navigation(c => c.Rules)
                .HasField("_rules")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}