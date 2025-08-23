using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlHub.Infrastructure.Persistence.Models;

namespace ControlHub.Infrastructure.Persistence.Configurations
{
    public class AccountConfig : IEntityTypeConfiguration<AccountEntity>
    {
        public void Configure(EntityTypeBuilder<AccountEntity> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Email)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(a => a.HashPassword)
                   .IsRequired();

            builder.Property(a => a.Salt)
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
