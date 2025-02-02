using api_be.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.Infrastructure.DB.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.InternalCode).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(100);
        }
    }
}
