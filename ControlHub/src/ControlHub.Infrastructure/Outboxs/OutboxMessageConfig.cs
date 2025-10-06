using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Outboxs
{
    public class OutboxMessageConfig : IEntityTypeConfiguration<OutboxMessageEntity>
    {
        public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Payload).IsRequired();
        }
    }
}