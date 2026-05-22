using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartInventory.API.Identity;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;

namespace SmartInventory.API.Data;

public static class DbSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var role in new[] { UserRole.Admin.ToString(), UserRole.Employee.ToString() })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!environment.IsDevelopment())
        {
            return;
        }

        var admin = await EnsureUserAsync(userManager, "admin", "Admin@123", UserRole.Admin);
        await EnsureUserAsync(userManager, "employee", "Employee@123", UserRole.Employee);

        if (!await db.Warehouses.AnyAsync())
        {
            db.Warehouses.AddRange(
                new Warehouse { Name = "Main Warehouse", Location = "HQ - Floor 1", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new Warehouse { Name = "Spare Parts", Location = "HQ - Floor 2", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-25) },
                new Warehouse { Name = "Outlet Store", Location = "Downtown", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(
                new Product { Name = "Barcode Scanner", SKU = "SCN-001", Category = "Hardware", Price = 59.99m, Description = "USB barcode scanner", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-40) },
                new Product { Name = "Thermal Label Printer", SKU = "PRN-010", Category = "Hardware", Price = 189.00m, Description = "4x6 thermal label printer", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-35) },
                new Product { Name = "Shipping Labels (Roll)", SKU = "LBL-100", Category = "Consumables", Price = 14.50m, Description = "500 labels per roll", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-33) },
                new Product { Name = "Packing Tape", SKU = "TAP-200", Category = "Consumables", Price = 3.25m, Description = "Heavy duty tape", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-32) },
                new Product { Name = "Storage Bin (Medium)", SKU = "BIN-050", Category = "Storage", Price = 7.99m, Description = "Plastic bin, medium size", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-29) },
                new Product { Name = "Safety Gloves", SKU = "GLV-020", Category = "Safety", Price = 2.49m, Description = "Work gloves", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-28) }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.InventoryTransactions.AnyAsync())
        {
            var mainWarehouseId = await db.Warehouses
                .Where(w => w.Name == "Main Warehouse")
                .Select(w => w.Id)
                .FirstAsync();

            var outletWarehouseId = await db.Warehouses
                .Where(w => w.Name == "Outlet Store")
                .Select(w => w.Id)
                .FirstAsync();

            var products = await db.Products
                .Select(p => new { p.Id, p.SKU })
                .ToListAsync();

            var bySku = products.ToDictionary(x => x.SKU, x => x.Id, StringComparer.OrdinalIgnoreCase);

            db.InventoryTransactions.AddRange(
                new InventoryTransaction
                {
                    ProductId = bySku["SCN-001"],
                    WarehouseId = mainWarehouseId,
                    Type = TransactionType.In,
                    Quantity = 20,
                    UnitPrice = 45.00m,
                    Notes = "Initial stock",
                    CreatedByUserId = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new InventoryTransaction
                {
                    ProductId = bySku["PRN-010"],
                    WarehouseId = mainWarehouseId,
                    Type = TransactionType.In,
                    Quantity = 8,
                    UnitPrice = 140.00m,
                    Notes = "Initial stock",
                    CreatedByUserId = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new InventoryTransaction
                {
                    ProductId = bySku["LBL-100"],
                    WarehouseId = mainWarehouseId,
                    Type = TransactionType.In,
                    Quantity = 120,
                    UnitPrice = 9.50m,
                    Notes = "Initial stock",
                    CreatedByUserId = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-9)
                },
                new InventoryTransaction
                {
                    ProductId = bySku["TAP-200"],
                    WarehouseId = mainWarehouseId,
                    Type = TransactionType.In,
                    Quantity = 200,
                    UnitPrice = 2.10m,
                    Notes = "Initial stock",
                    CreatedByUserId = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-9)
                },
                new InventoryTransaction
                {
                    ProductId = bySku["SCN-001"],
                    WarehouseId = outletWarehouseId,
                    Type = TransactionType.Out,
                    Quantity = 2,
                    UnitPrice = 0m,
                    Notes = "Transferred to outlet",
                    CreatedByUserId = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new InventoryTransaction
                {
                    ProductId = bySku["LBL-100"],
                    WarehouseId = mainWarehouseId,
                    Type = TransactionType.Out,
                    Quantity = 15,
                    UnitPrice = 0m,
                    Notes = "Used for shipments",
                    CreatedByUserId = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            );

            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string username,
        string password,
        UserRole role)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is not null)
        {
            if (!await userManager.IsInRoleAsync(user, role.ToString()))
            {
                await userManager.AddToRoleAsync(user, role.ToString());
            }

            return user;
        }

        user = new ApplicationUser { UserName = username, IsActive = true };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role.ToString());
        return user;
    }
}
