using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace api_be.DB.Configurations
{
    public class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : class
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.Property<DateTime>("CreatedAt").HasDefaultValueSql("GETDATE()");
            builder.Property<DateTime?>("UpdatedAt").HasDefaultValueSql("GETDATE()");



            //builder.Property(c => c.CreatedAt)
            //    .HasDefaultValueSql("GETDATE()")
            //    .IsRequired(false);

            //builder.Property(c => c.CreatedBy)
            //    .HasMaxLength(255)
            //    .IsRequired(false);

            //builder.Property(c => c.UpdatedAt)
            //    .HasDefaultValueSql("GETDATE()")
            //    .IsRequired(false);

            //builder.Property(c => c.UpdatedBy)
            //    .HasMaxLength(255)
            //    .IsRequired(false);

            //builder.Property(c => c.IsDeleted)
            //    .HasDefaultValue(false)
            //    .IsRequired();
        }
    }
}
