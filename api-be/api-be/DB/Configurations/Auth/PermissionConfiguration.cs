using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Entities.Auth;

namespace api_be.DB.Configurations.Auth
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).HasMaxLength(255);

            builder.HasMany(p => p.RolePermissions)
                   .WithOne(rp => rp.Permission)
                   .HasForeignKey(rp => rp.PermissionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.UserPermissions)
                   .WithOne(up => up.Permission)
                   .HasForeignKey(up => up.PermissionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
