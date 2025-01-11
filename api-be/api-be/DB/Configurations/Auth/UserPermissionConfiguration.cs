using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Auth;
using System.Reflection.Emit;

namespace api_be.DB.Configurations.Auth
{
    public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
    {
        public void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            builder.HasKey(up => up.Id);

            //builder.HasOne(up => up.User)
            //       .WithMany()
            //       .HasForeignKey(up => up.UserId)
            //       .OnDelete(DeleteBehavior.Cascade);

            //builder.HasOne(up => up.Permission)
            //       .WithMany()
            //       .HasForeignKey(up => up.PermissionId)
            //       .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình mối quan hệ giữa User và Permission
            builder.HasOne(up => up.User)
                .WithMany(u => u.UserPermissions) // Nếu User có nhiều UserPermission
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Cấu hình hành động khi xóa (nếu cần)

            builder.HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions) // Nếu Permission có nhiều UserPermission
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade); // Cấu hình hành động khi xóa (nếu cần)
        }
    }
}
