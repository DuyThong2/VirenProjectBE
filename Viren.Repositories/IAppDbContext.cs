using Microsoft.EntityFrameworkCore;
using Viren.Repositories.Domains;

namespace Viren.Repositories;

public interface IAppDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductDetail> ProductDetails { get; }

    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Payment> Payments { get; }

    DbSet<Wishlist> Wishlists { get; }

    DbSet<Sale> Sales { get; }
    DbSet<ProductSale> ProductSales { get; }

    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<UserSubscription> UserSubscriptions { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}