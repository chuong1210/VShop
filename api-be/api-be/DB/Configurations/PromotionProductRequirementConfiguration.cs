using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.DB.Configurations
{
    public class PromotionProductRequirementConfiguration : IEntityTypeConfiguration<PromotionProductRequirement>
    {
        public void Configure(EntityTypeBuilder<PromotionProductRequirement> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Promotion)
                   .WithMany()
                   .HasForeignKey(x => x.PromotionId);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId);
        }
    }

}
