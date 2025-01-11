using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api_be.DB.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(100);

            builder.Property(x => x.Email)
                   .HasMaxLength(100);

            builder.Property(x => x.Phone)
                   .HasMaxLength(20);

            builder.HasOne(x => x.User)
                   .WithOne()
                   .HasForeignKey<Customer>(x => x.Id);
        }
    }
}
