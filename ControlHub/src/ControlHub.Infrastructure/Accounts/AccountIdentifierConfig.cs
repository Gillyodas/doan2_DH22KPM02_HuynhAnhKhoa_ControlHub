using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Accounts
{
    public class AccountIdentifierConfig : IEntityTypeConfiguration<AccountIdentifierEntity>
    {
        public void Configure(EntityTypeBuilder<AccountIdentifierEntity> builder)
        {
            builder.ToTable("AccountIdentifiers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.Value).IsRequired().HasMaxLength(300);
            builder.Property(x => x.NormalizedValue).IsRequired().HasMaxLength(300);

            // Unique index on (Type, NormalizedValue)
            builder.HasIndex(x => new { x.Type, x.NormalizedValue }).IsUnique();

            builder.HasOne(x => x.Account)
                   .WithMany(a => a.Identifiers)
                   .HasForeignKey(x => x.AccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
