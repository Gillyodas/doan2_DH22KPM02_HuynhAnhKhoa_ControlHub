using ControlHub.Domain.Users;
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

            builder.Property(u => u.IsDeleted)
                .IsRequired();

            builder.Property(u => u.AccId)
                .IsRequired();

            // --- CẤU HÌNH RELATIONSHIP VỚI ACCOUNT ---
            // User thuộc về Account (1 Account có 1 hoặc nhiều User, tùy logic của bạn)
            // Giả sử 1 Account có 1 User (One-to-One) hoặc 1 Account có nhiều User (One-to-Many)

            // Nếu bạn định nghĩa Navigation Property bên phía Account (ví dụ: Account.Users)
            // Bạn có thể cấu hình ở đây hoặc bên AccountConfig.

            // Ví dụ cấu hình cơ bản (nếu User là Aggregate riêng lẻ):
            // builder.HasOne<Account>() // Có 1 Account
            //     .WithMany()           // Account có nhiều User (hoặc WithOne nếu 1-1)
            //     .HasForeignKey(u => u.AccId)
            //     .OnDelete(DeleteBehavior.Cascade); // Xóa Account thì xóa User
        }
    }
}