using api_be.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace api_be.DB.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            // Cấu hình bảng "Customers"
            builder.ToTable("Customers");

            // Cấu hình cột Id (khóa chính)
            builder.HasKey(c => c.Id);
            //builder.Property(c => c.Id)
            //    .IsRequired()
            //    .ValueGeneratedOnAdd(); // Tự động tăng

            // Cấu hình các cột khác
            builder.Property(c => c.Name)
                .HasMaxLength(255) // Nếu cần, có thể đặt giới hạn chiều dài
                .IsRequired(false); // Cột này có thể null

            builder.Property(c => c.Phone)
                .HasMaxLength(15) // Ví dụ giới hạn chiều dài cho số điện thoại
                .IsRequired(false);

            builder.Property(c => c.Email)
                .HasMaxLength(255) // Giới hạn chiều dài email
                .IsRequired(false);

            builder.Property(c => c.Address)
                .HasMaxLength(500) // Giới hạn chiều dài địa chỉ
                .IsRequired(false);

            builder.Property(c => c.Gender)
                .HasMaxLength(10) // Giới hạn chiều dài giới tính (nếu cần)
                .IsRequired(false);


            //// Cấu hình quan hệ khóa ngoại nếu cần
            //builder.HasOne(c => c.User)  // Nếu Customer có quan hệ với bảng Users
            //    .WithOne()  // Ở đây giả sử mỗi Customer chỉ có một User liên kết
            //    .HasForeignKey<Customer>(c => c.Id)  // Cột khóa ngoại trong bảng Customers
            //    .OnDelete(DeleteBehavior.SetNull);  // Xử lý hành vi khi xóa, nếu cần
        }
    }
}
