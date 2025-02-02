using api_be.Core.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api_be.Infrastructure.DB.Configurations.Auth
{
    public class InvalidatedTokenConfiguration : IEntityTypeConfiguration<InvalidatedToken>
    {
        public void Configure(EntityTypeBuilder<InvalidatedToken> builder)
        {
            builder.HasKey(x => new { x.JwtId, x.ExpiryTime });

            // Additional configuration options
            builder.Property(x => x.JwtId)
                .IsRequired()
                .HasMaxLength(500);  // Adjust the length as needed

            builder.Property(x => x.ExpiryTime)
                .IsRequired();

            // Optional: Add an index if you want to optimize queries
            builder.HasIndex(x => x.ExpiryTime);

            // Optional: Configure the table name if different from the default
            builder.ToTable("InvalidatedTokens");



        }

    }
}
