using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using api_be.Core.Entities.Auth;

namespace api_be.Infrastructure.DB.Configurations.Auth
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

       
            // Configure columns
            builder.HasKey(rt => rt.Id); // Assuming 'Id' is the primary key from AuditableEntity

            // Properties
            builder.Property(x => x.Id)
                .IsRequired()
                .ValueGeneratedOnAdd();
            builder.HasIndex(x => x.UserId);

            builder.Property(rt => rt.UserId)
                .IsRequired()
                .HasColumnName("UserId"); // Rename column if necessary

            builder.Property(rt => rt.Token)
                .IsRequired()

                .HasColumnName("Token")
                .HasMaxLength(500); // Define max length for token (adjust as needed)
                

            builder.Property(rt => rt.ExpiryDate)
                .IsRequired()
                .HasColumnName("ExpiryDate");

            builder.Property(rt => rt.IsUsed)
                .IsRequired()
                .HasColumnName("IsUsed");

            builder.HasIndex(x => x.Token)
      .IsUnique();
            builder.Property(rt => rt.IsRevoked)
                .IsRequired()
                .HasColumnName("IsRevoked");

            builder.Property(x => x.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("GETDATE()");


            // Configure relationships
            builder.HasOne(rt => rt.User)
                .WithMany() // Assuming User has a collection of RefreshTokens
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Delete the refresh token when the user is deleted
        }
    }
}
