using ECommerceWeb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceWeb.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        foreach (var role in new[] { AppRoles.Admin, AppRoles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        const string adminEmail = "admin@shop.local";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Quản trị viên",
                Address = "Hà Nội"
            };
            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }

        if (!await context.Categories.AnyAsync())
        {
            var cats = new[]
            {
                new Category { Name = "Điện tử", Description = "Thiết bị điện tử" },
                new Category { Name = "Thời trang", Description = "Quần áo, phụ kiện" },
                new Category { Name = "Gia dụng", Description = "Đồ dùng trong nhà" }
            };
            context.Categories.AddRange(cats);
            await context.SaveChangesAsync();

            var electronics = await context.Categories.FirstAsync(c => c.Name == "Điện tử");
            var fashion = await context.Categories.FirstAsync(c => c.Name == "Thời trang");
            var home = await context.Categories.FirstAsync(c => c.Name == "Gia dụng");

            context.Products.AddRange(
                new Product
                {
                    Name = "Tai nghe không dây",
                    Description = "Bluetooth 5.0, pin 24h",
                    Price = 890000,
                    Stock = 50,
                    CategoryId = electronics.Id,
                    ImageUrl = "/images/placeholder-product.svg"
                },
                new Product
                {
                    Name = "Chuột gaming",
                    Description = "Cảm biến quang, DPI 6400",
                    Price = 450000,
                    Stock = 30,
                    CategoryId = electronics.Id,
                    ImageUrl = "/images/placeholder-product.svg"
                },
                new Product
                {
                    Name = "Áo thun cotton",
                    Description = "100% cotton, nhiều màu",
                    Price = 199000,
                    Stock = 100,
                    CategoryId = fashion.Id,
                    ImageUrl = "/images/placeholder-product.svg"
                },
                new Product
                {
                    Name = "Bình giữ nhiệt",
                    Description = "Inox 500ml",
                    Price = 320000,
                    Stock = 40,
                    CategoryId = home.Id,
                    ImageUrl = "/images/placeholder-product.svg"
                }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Coupons.AnyAsync())
        {
            var utc = DateTime.UtcNow;
            context.Coupons.Add(new Coupon
            {
                Code = "SALE10",
                DiscountType = CouponDiscountType.Percent,
                Value = 10,
                MinOrderValue = 200_000,
                MaxDiscountAmount = 100_000,
                StartDateUtc = utc.AddDays(-1),
                EndDateUtc = utc.AddYears(1),
                UsageLimit = 1000,
                UsedCount = 0,
                IsActive = true
            });
            context.Coupons.Add(new Coupon
            {
                Code = "GIAM50K",
                DiscountType = CouponDiscountType.FixedAmount,
                Value = 50_000,
                MinOrderValue = 500_000,
                MaxDiscountAmount = null,
                StartDateUtc = utc.AddDays(-1),
                EndDateUtc = utc.AddYears(1),
                UsageLimit = null,
                UsedCount = 0,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        var sample = await context.Products.FirstOrDefaultAsync(p => p.Name == "Tai nghe không dây");
        if (sample is { DiscountPercent: null or 0 })
        {
            sample.DiscountPercent = 15;
            await context.SaveChangesAsync();
        }
    }
}
