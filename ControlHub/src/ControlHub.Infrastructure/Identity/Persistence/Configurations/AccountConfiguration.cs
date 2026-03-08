using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Entities;
using ControlHub.Domain.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Identity.Persistence.Configurations
{
    internal class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.IsActive).IsRequired();
            builder.Property(a => a.IsDeleted).IsRequired();

            // --- 1. C?U H�NH VALUE OBJECT: PASSWORD (Owned Entity) ---
            // EF Core s? nh�ng c�c c?t c?a Password v�o b?ng Accounts
            builder.OwnsOne(a => a.Password, passBuilder =>
            {
                passBuilder.Property(p => p.Hash)
                    .HasColumnName("HashPassword") // T�n c?t trong DB
                    .HasColumnType("varbinary(64)")
                    .IsRequired();

                passBuilder.Property(p => p.Salt)
                    .HasColumnName("Salt") // T�n c?t trong DB
                    .HasColumnType("varbinary(64)")
                    .IsRequired();
            });

            // --- 2. C?U H�NH RELATIONSHIPS ---

            // Account (1) -> Role (1)
            builder.HasOne(a => a.Role)
                .WithMany() // Ho?c .WithMany(r => r.Accounts) n?u b�n Role c� list Accounts
                .HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Account (1) -> User (1)
            builder.HasOne(a => a.User)
                .WithOne() // Ho?c .WithOne(u => u.Account) n?u b�n User c� prop Account
                .HasForeignKey<User>(u => u.AccId) // User gi? kh�a ngo?i AccId
                .OnDelete(DeleteBehavior.Cascade);

            // Account (1) -> Tokens (N)
            builder.HasMany(a => a.Tokens)
                .WithOne() // B�n Token d� c?u h�nh HasOne<Account>
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Access Mode cho Tokens
            builder.Navigation(a => a.Tokens)
                .HasField("_tokens")
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // --- 3. C?U H�NH OWNED COLLECTION: IDENTIFIERS ---
            // Map List<Identifier> (VO) sang b?ng ri�ng "AccountIdentifiers"
            // EF Core s? t? t?o Shadow PK cho b?ng n�y.
            builder.OwnsMany(a => a.Identifiers, ib =>
            {
                ib.ToTable("AccountIdentifiers");

                ib.WithOwner().HasForeignKey("AccountId"); // FK tr? v? Account

                // Map c�c property c?a Identifier
                ib.Property(i => i.Name).IsRequired().HasMaxLength(100);
                ib.Property(i => i.Type).IsRequired();
                ib.Property(i => i.Value).IsRequired().HasMaxLength(300);
                ib.Property(i => i.NormalizedValue).IsRequired().HasMaxLength(300);
                ib.Property(i => i.IsDeleted).HasDefaultValue(false);

                // T?o Unique Index tr�n b?ng ph? - s? d?ng Name thay v� Type
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
