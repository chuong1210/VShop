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
        }
    }
}
