using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartInventory.Application.DTOs.Common;
using SmartInventory.Application.DTOs.Products;
using SmartInventory.Application.Exceptions;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IUnitOfWork unitOfWork, IMemoryCache cache, ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new NotFoundException("Product not found");

        var cacheKey = $"product:{id}";
        if (_cache.TryGetValue(cacheKey, out ProductDto? cached) && cached is not null)
        {
            return cached;
        }

        var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
        if (product is null) throw new NotFoundException("Product not found");

        var dto = MapToDto(product);
        _cache.Set(cacheKey, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return dto;
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "product_categories";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cached) && cached is not null)
        {
            return cached;
        }

        var categories = await _unitOfWork.Products.GetCategoriesAsync(cancellationToken);
        _cache.Set(cacheKey, categories, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });

        return categories;
    }

    public async Task<PagedResultDto<ProductDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        string? category,
        bool? isActive,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = NormalizePageSize(pageSize, 10, 100);

        var (items, total) = await _unitOfWork.Products.GetPagedAsync(
            page,
            pageSize,
            search,
            category,
            isActive,
            minPrice,
            maxPrice,
            cancellationToken);
        return new PagedResultDto<ProductDto>
        {
            Items = items.Select(MapToDto).ToArray(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var sku = dto.SKU.Trim();

        if (await _unitOfWork.Products.SKUExistsAsync(sku, null, cancellationToken))
        {
            throw new ConflictException("SKU already exists");
        }

        var entity = new Product
        {
            Name = dto.Name.Trim(),
            SKU = sku,
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            Category = dto.Category?.Trim(),
            IsActive = true
        };

        await _unitOfWork.Products.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created. ProductId={ProductId} SKU={SKU}", entity.Id, entity.SKU);
        return MapToDto(entity);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new NotFoundException("Product not found");

        var entity = await _unitOfWork.Products.GetByIdIncludingInactiveAsync(id, cancellationToken);
        if (entity is null) throw new NotFoundException("Product not found");

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.Price = dto.Price;
        entity.Category = dto.Category?.Trim();
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Products.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.Remove($"product:{id}");

        _logger.LogInformation("Product updated. ProductId={ProductId}", entity.Id);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new NotFoundException("Product not found");

        var entity = await _unitOfWork.Products.GetByIdIncludingInactiveAsync(id, cancellationToken);
        if (entity is null) throw new NotFoundException("Product not found");

        if (!entity.IsActive) return;

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Products.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.Remove($"product:{id}");
        _logger.LogInformation("Product soft-deleted. ProductId={ProductId}", entity.Id);
    }

    private static ProductDto MapToDto(Product entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            SKU = entity.SKU,
            Description = entity.Description,
            Price = entity.Price,
            Category = entity.Category,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAt
        };

    private static int NormalizePageSize(int pageSize, int defaultSize, int maxSize)
    {
        if (pageSize <= 0) return defaultSize;
        if (pageSize > maxSize) return maxSize;
        return pageSize;
    }
}
