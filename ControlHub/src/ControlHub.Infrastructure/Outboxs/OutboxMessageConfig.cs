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

            // Lưu Enum dưới dạng String để dễ đọc trong DB (và khớp với MaxLength cũ)
            builder.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Payload)
                .IsRequired();

            builder.Property(x => x.Processed)
                .IsRequired();

            // Có thể thêm Index cho Processed để worker job tìm nhanh hơn
            builder.HasIndex(x => x.Processed)
                .HasFilter("[Processed] = 0"); // Chỉ index những cái chưa xử lý (SQL Server)
        }
    }
}