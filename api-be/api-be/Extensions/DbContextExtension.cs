
using api_be.Common.Interfaces;
using api_be.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace api_be.Extensions
{
    public static class DbContextExtension
    {
        public static IQueryable<T> FilterDeleted<T>(this DbSet<T> dbSet) where T : AuditableEntity
        {
            return dbSet.Where(e => (bool)!e.IsDeleted);
        }

        public static Task<int> SaveChangesAsync(this ISupermarketDbContext context)
        {
            return context.SaveChangesAsync(CancellationToken.None);
        }
    }
}
