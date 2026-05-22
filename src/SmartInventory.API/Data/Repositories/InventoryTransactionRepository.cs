using Microsoft.EntityFrameworkCore;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.API.Data.Repositories;

public class InventoryTransactionRepository : Repository<InventoryTransaction>, IInventoryTransactionRepository
{
    public InventoryTransactionRepository(AppDbContext context) : base(context)
    {
    }

    public Task<InventoryTransaction?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking()
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<int> GetCurrentStockAsync(int productId, int warehouseId, CancellationToken cancellationToken = default)
    {
        var inQty = await DbSet.AsNoTracking()
            .Where(t => t.ProductId == productId && t.WarehouseId == warehouseId && t.Type == TransactionType.In)
            .SumAsync(t => (int?)t.Quantity, cancellationToken) ?? 0;

        var outQty = await DbSet.AsNoTracking()
            .Where(t => t.ProductId == productId && t.WarehouseId == warehouseId && t.Type == TransactionType.Out)
            .SumAsync(t => (int?)t.Quantity, cancellationToken) ?? 0;

        return inQty - outQty;
    }

    public async Task<(IReadOnlyList<InventoryTransaction> Items, int TotalCount)> GetHistoryPagedAsync(
        int page,
        int pageSize,
        int? productId,
        int? warehouseId,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(t => t.ProductId == productId.Value);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(t => t.WarehouseId == warehouseId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

