using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Domain.Entities;

namespace api_be.DB.Configurations
{
    public class StaffConfiguration : IEntityTypeConfiguration<Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.InternalCode).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(100);
            builder.Property(x => x.Gender).HasMaxLength(10);
            builder.Property(x => x.PhoneNumber).HasMaxLength(15);
            builder.Property(x => x.Email).HasMaxLength(100);
            builder.Property(x => x.IdCard).HasMaxLength(20);

            builder.HasOne(x => x.Position)
                   .WithMany()
                   .HasForeignKey(x => x.PositionId);

            builder.HasOne(x => x.User)
                   .WithOne()
                   .HasForeignKey<Staff>(x => x.Id);
        }
    }
}
