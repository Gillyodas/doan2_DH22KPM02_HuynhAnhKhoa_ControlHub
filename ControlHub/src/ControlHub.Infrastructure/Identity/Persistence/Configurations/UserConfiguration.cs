using ControlHub.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Identity.Persistence.Configurations
{
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username)
                .HasMaxLength(100);

            builder.Property(u => u.FirstName)
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .HasMaxLength(100);

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20);

            builder.Property(u => u.IsDeleted)
                .IsRequired();

            builder.Property(u => u.AccId)
                .IsRequired();

            // --- C?U H�NH RELATIONSHIP V?I ACCOUNT ---
            // User thu?c v? Account (1 Account c� 1 ho?c nhi?u User, t�y logic c?a b?n)
            // Gi? s? 1 Account c� 1 User (One-to-One) ho?c 1 Account c� nhi?u User (One-to-Many)

            // N?u b?n d?nh nghia Navigation Property b�n ph�a Account (v� d?: Account.Users)
            // B?n c� th? c?u h�nh ? d�y ho?c b�n AccountConfig.

            // V� d? c?u h�nh co b?n (n?u User l� Aggregate ri�ng l?):
            // builder.HasOne<Account>() // C� 1 Account
            //     .WithMany()           // Account c� nhi?u User (ho?c WithOne n?u 1-1)
            //     .HasForeignKey(u => u.AccId)
            //     .OnDelete(DeleteBehavior.Cascade); // X�a Account th� x�a User
        }
    }
}
