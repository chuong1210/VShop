using api_be.Core.Domain;
using api_be.Core.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using api_be.Infrastructure.DB;

namespace api_be.Domain.Extensions
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
