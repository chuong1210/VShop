using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.DB.Configurations
{
    public class DistributorConfiguration : IEntityTypeConfiguration<Distributor>
    {
        public void Configure(EntityTypeBuilder<Distributor> builder)
        {
            builder.ToTable("Distributors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.InternalCode).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(100);
            builder.Property(x => x.Email).HasMaxLength(100);
            builder.Property(x => x.Phone).HasMaxLength(20);
        }
    }

}
