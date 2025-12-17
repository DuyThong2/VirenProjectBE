using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viren.Repositories.Domains;

namespace Viren.Repositories
{
    public class AppDbContext
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ========= DbSet =========
        //public DbSet<User> Users => Set<User>();

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductDetail> ProductDetails => Set<ProductDetail>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<Wishlist> Wishlists => Set<Wishlist>();

        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<ProductSale> ProductSales => Set<ProductSale>();

        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();


        protected override void OnModelCreating(ModelBuilder builder)
        {
            // ⚠️ BẮT BUỘC với Identity
            base.OnModelCreating(builder);

            // Áp dụng toàn bộ IEntityTypeConfiguration<T>
            builder.ApplyConfigurationsFromAssembly(
                typeof(AppDbContext).Assembly
            );
        }
    }


}
