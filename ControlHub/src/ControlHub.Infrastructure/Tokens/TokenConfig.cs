using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Tokens
{
    public class TokenConfig : IEntityTypeConfiguration<TokenEntity>
    {
        public void Configure(EntityTypeBuilder<TokenEntity> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Type).HasMaxLength(50).IsRequired();

            builder.HasOne(t => t.Account)
                  .WithMany(a => a.Tokens)
                  .HasForeignKey(t => t.AccountId)
                  .OnDelete(DeleteBehavior.Cascade);

            builder.Property(t => t.Value)
                   .HasMaxLength(512) // JWT, GUID, random string… thường < 512 chars
                   .IsRequired();

            builder.HasIndex(t => t.ExpiredAt);
        }
    }
}
