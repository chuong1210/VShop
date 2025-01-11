using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.DB.Configurations
{
    public class DetailOrderConfiguration : IEntityTypeConfiguration<DetailOrder>
    {
        public void Configure(EntityTypeBuilder<DetailOrder> builder)
        {
            builder.ToTable("DetailOrders");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Order)
                   .WithMany()
                   .HasForeignKey(x => x.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
