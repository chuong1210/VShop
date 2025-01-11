using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Domain.Entities;

namespace api_be.DB.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.InternalCode).HasMaxLength(50);
            builder.Property(x => x.Message).HasMaxLength(500);

            builder.HasOne(x => x.Payment)
                   .WithMany()
                   .HasForeignKey(x => x.PaymentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Coupon)
                   .WithMany()
                   .HasForeignKey(x => x.CouponId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Delivery)
                   .WithMany()
                   .HasForeignKey(x => x.DeliveryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.StaffApproved)
                   .WithMany()
                   .HasForeignKey(x => x.StaffApprovedId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
