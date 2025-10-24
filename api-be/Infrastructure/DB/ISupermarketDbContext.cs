
using Microsoft.EntityFrameworkCore;
using api_be.Core.Entities;
using api_be.Core.Entities.Auth;

namespace api_be.Infrastructure.DB
{
    public interface ISupermarketDbContext
    {
        DbSet<User> Users { get; }

        DbSet<Role> Roles { get; }

        DbSet<Permission> Permissions { get; }

        DbSet<UserRole> UserRoles { get; }

        DbSet<RolePermission> RolePermissions { get; }

        DbSet<UserPermission> UserPermissions { get; }

        DbSet<Staff> Staffs { get; }

        DbSet<Customer> Customers { get; }

        DbSet<Distributor> Distributors { get; }

        DbSet<Category> Categories { get; }

        DbSet<Product> Products { get; }

        DbSet<SupplierOrder> SupplierOrders { get; }

        DbSet<DetailSupplierOrder> DetailSupplierOrders { get; }

        DbSet<Payment> Payments { get; }

        DbSet<StaffPosition> StaffPositions { get; }

        DbSet<Promotion> Promotions { get; }

        DbSet<PromotionProductRequirement> PromotionProductRequirements { get; }

        DbSet<Coupon> Coupons { get; }

        DbSet<Order> Orders { get; }

        DbSet<Delivery> Deliveries { get; }

        DbSet<DetailOrder> DetailOrders { get; }

        DbSet<StaffPositionHasRole> StaffPositionHasRoles { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<UserVerification> UserVerifications { get; }

        DbSet<InvalidatedToken> InvalidatedTokens { get; }



        DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
