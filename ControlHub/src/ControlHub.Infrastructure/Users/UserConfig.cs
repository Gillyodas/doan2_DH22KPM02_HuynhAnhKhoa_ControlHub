using ControlHub.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Users
{
    internal class UserConfig : IEntityTypeConfiguration<User>
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

            // --- C?U HÌNH RELATIONSHIP V?I ACCOUNT ---
            // User thu?c v? Account (1 Account có 1 ho?c nhi?u User, tùy logic c?a b?n)
            // Gi? s? 1 Account có 1 User (One-to-One) ho?c 1 Account có nhi?u User (One-to-Many)

            // N?u b?n d?nh nghia Navigation Property bên phía Account (ví d?: Account.Users)
            // B?n có th? c?u hình ? dây ho?c bên AccountConfig.

            // Ví d? c?u hình co b?n (n?u User là Aggregate riêng l?):
            // builder.HasOne<Account>() // Có 1 Account
            //     .WithMany()           // Account có nhi?u User (ho?c WithOne n?u 1-1)
            //     .HasForeignKey(u => u.AccId)
            //     .OnDelete(DeleteBehavior.Cascade); // Xóa Account thì xóa User
        }
    }
}
