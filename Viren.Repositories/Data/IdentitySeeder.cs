using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Viren.Repositories.Domains;

namespace Viren.Repositories.Data;

public class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // ===== Ensure DB created =====
        await context.Database.MigrateAsync();

        // ===== Seed Roles =====
        string[] roles = { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    NormalizedName = role.ToUpper()
                });
            }
        }

        // ===== Seed Admin User =====
        if (!await userManager.Users.AnyAsync())
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                UserName = "admin123",
                Email = "admin@viren.com",
                NormalizedUserName = "ADMIN@VIREN.COM",
                NormalizedEmail = "ADMIN@VIREN.COM",
                EmailConfirmed = true,
                FirstName = "System",
                Name = "System Admin",           

                LastName = "Admin",
                CreatedAt = DateTime.UtcNow,
                Status = Viren.Repositories.Enums.CommonStatus.Active
            };

            var adminResult = await userManager.CreateAsync(admin, "Admin@123");

            if (adminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ===== Seed Normal User =====
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user123",
                Email = "user@viren.com",
                NormalizedUserName = "USER@VIREN.COM",
                NormalizedEmail = "USER@VIREN.COM",
                EmailConfirmed = true,
                FirstName = "Normal",
                LastName = "User",
                Name = "System user",            

                CreatedAt = DateTime.UtcNow,
                Status = Viren.Repositories.Enums.CommonStatus.Active
            };

            var userResult = await userManager.CreateAsync(user, "User@123");

            if (userResult.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");
            }
        }
    }
}