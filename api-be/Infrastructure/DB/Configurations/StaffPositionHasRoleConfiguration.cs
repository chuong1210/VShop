using api_be.Core.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.Infrastructure.DB.Configurations
{
    public class StaffPositionHasRoleConfiguration : IEntityTypeConfiguration<StaffPositionHasRole>
    {
        public void Configure(EntityTypeBuilder<StaffPositionHasRole> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Role)
                   .WithMany()
                   .HasForeignKey(x => x.RoleId);

            builder.HasOne(x => x.StaffPosition)
                   .WithMany()
                   .HasForeignKey(x => x.StaffPositionId);
        }
    }
}
