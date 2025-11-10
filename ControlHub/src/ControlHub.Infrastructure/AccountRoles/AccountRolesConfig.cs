using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.AccountRoles
{
    public class AccountRoleConfig : IEntityTypeConfiguration<AccountRoleEntity>
    {
        public void Configure(EntityTypeBuilder<AccountRoleEntity> builder)
        {
            builder.ToTable("AccountRoles");

            builder.HasKey(ar => new { ar.AccountId, ar.RoleId });

            builder.HasOne(ar => ar.Account)
                .WithMany(a => a.AccountRoles)
                .HasForeignKey(ar => ar.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ar => ar.Role)
                .WithMany(r => r.AccountRoles)
                .HasForeignKey(ar => ar.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}