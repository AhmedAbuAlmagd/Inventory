using SmartInventory.Application.DTOs.Common;
using SmartInventory.Application.DTOs.Products;

namespace SmartInventory.Application.Interfaces;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProductDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        string? category,
        bool? isActive,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
