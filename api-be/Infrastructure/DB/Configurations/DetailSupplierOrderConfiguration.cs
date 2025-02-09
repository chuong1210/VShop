using api_be.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.Infrastructure.DB.Configurations
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

                    //.OnDelete(DeleteBehavior.Cascade); // Kích hoạt Cascade Delete tự xóa luôn dữ liệu cha

        }
    }
}
