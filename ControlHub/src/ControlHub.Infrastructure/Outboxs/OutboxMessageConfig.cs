using ControlHub.Domain.Outboxs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Outboxs
{
    internal class OutboxMessageConfig : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(x => x.Id);

            // Luu Enum du?i d?ng String d? d? d?c trong DB (và kh?p v?i MaxLength cu)
            builder.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Payload)
                .IsRequired();

            builder.Property(x => x.Processed)
                .IsRequired();

            // Có th? thêm Index cho Processed d? worker job tìm nhanh hon
            builder.HasIndex(x => x.Processed)
                .HasFilter("[Processed] = 0"); // Ch? index nh?ng cái chua x? lý (SQL Server)
        }
    }
}
