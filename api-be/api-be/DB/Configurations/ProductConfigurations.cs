using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Domain.Entities;

namespace api_be.DB.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.InternalCode).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(100);
            builder.Property(x => x.Images).HasMaxLength(500);
            builder.Property(x => x.Describes).HasMaxLength(1000);
            builder.Property(x => x.Feature).HasMaxLength(500);
            builder.Property(x => x.Specifications).HasMaxLength(500);

            builder.HasOne(x => x.Parent)
                   .WithMany()
                   .HasForeignKey(x => x.ParentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Category)
                   .WithMany()
                   .HasForeignKey(x => x.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
