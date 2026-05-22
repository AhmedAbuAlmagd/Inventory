using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartInventory.API.Identity;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;

namespace SmartInventory.API.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(b =>
        {
            b.HasIndex(p => p.SKU).IsUnique();
            b.Property(p => p.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InventoryTransaction>(b =>
        {
            b.Property(t => t.UnitPrice).HasPrecision(18, 2);
            b.Property(t => t.Type).HasConversion<string>();

            b.HasOne(t => t.Product)
                .WithMany(p => p.Transactions)
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.Warehouse)
                .WithMany(w => w.Transactions)
                .HasForeignKey(t => t.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(x => x.TokenHash).IsUnique();
            b.HasIndex(x => x.UserId);

            b.Property(x => x.TokenHash).HasMaxLength(64);
            b.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.HasIndex(x => x.UserName).IsUnique();
        });
    }
}
