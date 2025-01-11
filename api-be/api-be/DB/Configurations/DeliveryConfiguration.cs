using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.DB.Configurations
{
    public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
    {
        public void Configure(EntityTypeBuilder<Delivery> builder)
        {
            builder.ToTable("Deliveries");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.InternalCode)
                   .HasMaxLength(50);

            builder.Property(x => x.From)
                   .HasMaxLength(200);

            builder.Property(x => x.To)
                   .HasMaxLength(200);

            builder.HasOne(x => x.PackingStaff)
                   .WithMany()
                   .HasForeignKey(x => x.PackingStaffId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Shipper)
                   .WithMany()
                   .HasForeignKey(x => x.ShipperId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
