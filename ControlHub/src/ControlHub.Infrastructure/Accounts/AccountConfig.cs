using ControlHub.Domain.Accounts;
using ControlHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Accounts
{
    internal class AccountConfig : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.IsActive).IsRequired();
            builder.Property(a => a.IsDeleted).IsRequired();

            // --- 1. CẤU HÌNH VALUE OBJECT: PASSWORD (Owned Entity) ---
            // EF Core sẽ nhúng các cột của Password vào bảng Accounts
            builder.OwnsOne(a => a.Password, passBuilder =>
            {
                passBuilder.Property(p => p.Hash)
                    .HasColumnName("HashPassword") // Tên cột trong DB
                    .HasColumnType("varbinary(64)")
                    .IsRequired();

                passBuilder.Property(p => p.Salt)
                    .HasColumnName("Salt") // Tên cột trong DB
                    .HasColumnType("varbinary(64)")
                    .IsRequired();
            });

            // --- 2. CẤU HÌNH RELATIONSHIPS ---

            // Account (1) -> Role (1)
            builder.HasOne(a => a.Role)
                .WithMany() // Hoặc .WithMany(r => r.Accounts) nếu bên Role có list Accounts
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Account (1) -> User (1)
            builder.HasOne(a => a.User)
                .WithOne() // Hoặc .WithOne(u => u.Account) nếu bên User có prop Account
                .HasForeignKey<User>(u => u.AccId) // User giữ khóa ngoại AccId
                .OnDelete(DeleteBehavior.Cascade);

            // Account (1) -> Tokens (N)
            builder.HasMany(a => a.Tokens)
                .WithOne() // Bên Token đã cấu hình HasOne<Account>
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Access Mode cho Tokens
            builder.Navigation(a => a.Tokens)
                .HasField("_tokens")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // --- 3. CẤU HÌNH OWNED COLLECTION: IDENTIFIERS ---
            // Map List<Identifier> (VO) sang bảng riêng "AccountIdentifiers"
            // EF Core sẽ tự tạo Shadow PK cho bảng này.
            builder.OwnsMany(a => a.Identifiers, ib =>
            {
                ib.ToTable("AccountIdentifiers");

                ib.WithOwner().HasForeignKey("AccountId"); // FK trỏ về Account

                // Map các property của Identifier
                ib.Property(i => i.Name).IsRequired().HasMaxLength(100);
                ib.Property(i => i.Type).IsRequired();
                ib.Property(i => i.Value).IsRequired().HasMaxLength(300);
                ib.Property(i => i.NormalizedValue).IsRequired().HasMaxLength(300);
                ib.Property(i => i.IsDeleted).HasDefaultValue(false);

                // Tạo Unique Index trên bảng phụ - sử dụng Name thay vì Type
                ib.HasIndex(i => new { i.Name, i.NormalizedValue })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            });

            // Access Mode cho Identifiers
            builder.Navigation(a => a.Identifiers)
                .HasField("_identifiers")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}