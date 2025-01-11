using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.DB.Configurations
{
    public class StaffPositionConfiguration : IEntityTypeConfiguration<StaffPosition>
    {
        public void Configure(EntityTypeBuilder<StaffPosition> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.InternalCode).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(100);
        }
    }
}
