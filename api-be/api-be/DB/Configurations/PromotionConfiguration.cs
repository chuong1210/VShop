using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Domain.Entities;

namespace api_be.DB.Configurations
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.ToTable("Promotions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.InternalCode).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(100);

            builder.Property(x => x.Discount).HasColumnType("decimal(18,2)");
            builder.Property(x => x.Percent).HasColumnType("decimal(5,2)");
        }
    }
}
