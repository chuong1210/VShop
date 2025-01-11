using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.DB.Configurations
{
    public class DetailSupplierOrderConfiguration : IEntityTypeConfiguration<DetailSupplierOrder>
    {
        public void Configure(EntityTypeBuilder<DetailSupplierOrder> builder)
        {
            builder.ToTable("DetailSupplierOrders");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.SupplierOrder)
                   .WithMany()
                   .HasForeignKey(x => x.SupplierOrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
