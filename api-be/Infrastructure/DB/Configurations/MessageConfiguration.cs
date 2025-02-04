//using api_be.Core.Entities;
//using api_be.Core.Entities.Auth;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;

//namespace api_be.Infrastructure.DB.Configurations
//{
//    public class MessageConfiguration:IEntityTypeConfiguration<Message>
//    {
//         public void Configure(EntityTypeBuilder<Message> builder)
//    {
//        // Đặt tên bảng
//        builder.ToTable("Messages");

//            // Khóa chính
//            builder.HasKey(m => m.Id);

//            // Thuộc tính Id
//            builder.Property(m => m.Id)
//                .ValueGeneratedOnAdd()
//                .IsRequired();

//        // Thuộc tính SenderId
//        builder.Property(m => m.SenderId)
//                .HasMaxLength(50) // Điều chỉnh độ dài phù hợp với yêu cầu của bạn
//                .IsRequired();

//        // Thuộc tính ReceiverId
//        builder.Property(m => m.ReceiverId)
//                .HasMaxLength(50) // Điều chỉnh độ dài phù hợp với yêu cầu của bạn
//                .IsRequired();

//        // Thuộc tính Content
//        builder.Property(m => m.Content)
//                .HasMaxLength(1000) // Điều chỉnh độ dài tối đa của tin nhắn
//                .IsRequired();

//        // Thuộc tính SentAt
//        builder.Property(m => m.SentAt)
//                .IsRequired();

//        // Thuộc tính IsRead
//        builder.Property(m => m.IsRead)
//                .HasDefaultValue(false)
//                .IsRequired();

//        // Index cho SenderId và ReceiverId để tối ưu hóa tìm kiếm
//        builder.HasIndex(m => m.SenderId);
//            builder.HasIndex(m => m.ReceiverId);

//           // Tùy chọn: Thêm mô tả quan hệ nếu SenderId và ReceiverId liên kết với bảng User
//             builder.HasOne<User>()
//                 .WithMany()
//                 .HasForeignKey(m => m.SenderId)
//                 .OnDelete(DeleteBehavior.Restrict);

//            builder.HasOne<User>()
//                .WithMany()
//                .HasForeignKey(m => m.ReceiverId)
//                .OnDelete(DeleteBehavior.Restrict);
//        }
//}
//}
