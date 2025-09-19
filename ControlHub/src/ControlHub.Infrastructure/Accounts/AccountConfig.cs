using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Infrastructure.Users;

namespace ControlHub.Infrastructure.Accounts
{
    public class AccountConfig : IEntityTypeConfiguration<AccountEntity>
    {
        public void Configure(EntityTypeBuilder<AccountEntity> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Email)
                    .HasConversion(
                    email => email.Value,           // Từ VO Email → string để lưu DB
                    value => Email.UnsafeCreate(value)   // Từ string DB → VO Email
                    )
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(a => a.HashPassword)
                   .HasColumnType("varbinary(64)")
                   .IsRequired();

            builder.Property(a => a.Salt)
                   .HasColumnType("varbinary(64)")
                   .IsRequired();

            builder.Property(a => a.IsActive)
                   .IsRequired();

            builder.Property(a => a.IsDeleted)
                   .IsRequired();

            // 1-1: Account <-> User
            builder.HasOne(a => a.User)
                   .WithOne(u => u.Account)
                   .HasForeignKey<UserEntity>(u => u.AccId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
