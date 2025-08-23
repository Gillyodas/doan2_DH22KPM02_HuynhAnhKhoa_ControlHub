using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ControlHub.Infrastructure.Persistence.Models;

namespace ControlHub.Infrastructure.Persistence.Configs
{
    public class UserConfig : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username)
                   .HasMaxLength(100);

            builder.Property(u => u.IsDeleted)
                   .IsRequired();

            builder.Property(u => u.AccId)
                   .IsRequired();
        }
    }
}
