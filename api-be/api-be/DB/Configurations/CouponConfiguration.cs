using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api_be.DB.Configurations
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.ToTable("Coupons");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.InternalCode)
                   .HasMaxLength(50);

            builder.Property(x => x.Name)
                   .HasMaxLength(100);

            builder.HasOne(x => x.Customer)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
