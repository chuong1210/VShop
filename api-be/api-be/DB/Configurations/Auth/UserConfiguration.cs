using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Entities.Auth;

namespace api_be.DB.Configurations.Auth
{

    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {

            //// Chỉ định khóa chính sử dụng Guid (UUID)
            //builder.HasKey(x => x.Id);

            //builder.Property(x => x.Id)
            //       .HasDefaultValueSql("NEWID()")  // Tạo GUID mặc định nếu không có giá trị nào được chỉ định
            //       .ValueGeneratedOnAdd();  // GUID tự động tạo khi thêm mới


         
            builder.HasKey(u => u.Id);
            //// Cấu hình Id tăng dần tự động
            builder.Property(x => x.Id)
                   .ValueGeneratedOnAdd(); // Thiết lập Id tự động tăng dần
            builder.Property(u => u.UserName).HasMaxLength(255);
            builder.Property(u => u.Password).HasMaxLength(255);
            builder.Property(u => u.Email).HasMaxLength(255);
            builder.Property(u => u.PhoneNumber).HasMaxLength(20);

            builder.HasMany(u => u.UserRoles)
                   .WithOne(ur => ur.User)
                   .HasForeignKey(ur => ur.UserId)
                   .OnDelete(DeleteBehavior.Cascade);


            builder.HasOne(u => u.Staff)
                   .WithOne(up=>up.User)
                   .HasForeignKey<User>(u => u.StaffId)
                   .OnDelete(DeleteBehavior.Restrict).IsRequired(false);  // Chỉ ra rằng StaffId có thể null


            builder.HasOne(u => u.Customer)
                   .WithOne(up=>up.User)
                   .HasForeignKey<User>(u => u.CustomerId)  // Đảm bảo chỉ có một khóa ngoại.
                   .OnDelete(DeleteBehavior.Restrict).IsRequired(false);  // Chỉ ra rằng CustomerID có thể null


            builder.HasMany(u => u.UserPermissions)
                   .WithOne(up => up.User)
                   .HasForeignKey(up => up.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
