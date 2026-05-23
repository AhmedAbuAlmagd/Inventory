using SmartInventory.Domain.Entities;

namespace SmartInventory.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<bool> SKUExistsAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdIncludingInactiveAsync(int id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        string? category,
        bool? isActive,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default);
}
