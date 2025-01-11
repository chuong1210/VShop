using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Domain.Entities;

namespace api_be.DB.Configurations
{
    public class SupplierOrderConfiguration : IEntityTypeConfiguration<SupplierOrder>
    {
        public void Configure(EntityTypeBuilder<SupplierOrder> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.InternalCode).HasMaxLength(50);
            builder.Property(x => x.Deliver).HasMaxLength(100);

            builder.HasOne(x => x.Parent)
                   .WithMany()
                   .HasForeignKey(x => x.ParentId);

            builder.HasOne(x => x.ApproveStaff)
                   .WithMany()
                   .HasForeignKey(x => x.ApproveStaffId);

            builder.HasOne(x => x.Distributor)
                   .WithMany()
                   .HasForeignKey(x => x.DistributorId);
        }
    }
}
