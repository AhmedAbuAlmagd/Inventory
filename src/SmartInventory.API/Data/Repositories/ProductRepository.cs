using Microsoft.EntityFrameworkCore;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.API.Data.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context)
    {
    }

    public override Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking().Where(p => p.IsActive).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> SKUExistsAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        return DbSet.AsNoTracking()
            .AnyAsync(p => p.SKU == sku && (excludeId == null || p.Id != excludeId), cancellationToken);
    }

    public Task<Product?> GetByIdIncludingInactiveAsync(int id, CancellationToken cancellationToken = default) =>
        DbSet.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(p => p.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.SKU.ToLower().Contains(term) ||
                (p.Category != null && p.Category.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
