using ControlHub.Application.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ControlHub.Infrastructure.Persistence.Configurations
{
    internal class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(x => x.Id);

            // Luu Enum du?i d?ng String d? d? d?c trong DB (v� kh?p v?i MaxLength cu)
            builder.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Payload)
                .IsRequired();

            builder.Property(x => x.Processed)
                .IsRequired();

            // C� th? th�m Index cho Processed d? worker job t�m nhanh hon
            builder.HasIndex(x => x.Processed)
                .HasFilter("[Processed] = 0"); // Ch? index nh?ng c�i chua x? l� (SQL Server)
        }
    }
}
