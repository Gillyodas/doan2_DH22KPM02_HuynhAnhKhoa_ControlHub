using ControlHub.Infrastructure.Accounts;
using ControlHub.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AccountConfig : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

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

        // 1-N: Account <-> Identifiers
        builder.HasMany(a => a.Identifiers)
               .WithOne(i => i.Account)
               .HasForeignKey(i => i.AccountId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}