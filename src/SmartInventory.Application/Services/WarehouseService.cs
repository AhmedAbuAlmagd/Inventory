using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartInventory.Application.DTOs.Warehouses;
using SmartInventory.Application.Exceptions;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WarehouseService> _logger;

    public WarehouseService(IUnitOfWork unitOfWork, IMemoryCache cache, ILogger<WarehouseService> logger)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WarehouseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "warehouses:all";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<WarehouseDto>? cached) && cached is not null)
        {
            return cached;
        }

        var warehouses = await _unitOfWork.Warehouses.GetAllAsync(cancellationToken);
        var dtos = warehouses.Select(MapToDto).ToArray();

        _cache.Set(cacheKey, dtos, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });

        return dtos;
    }

    public async Task<WarehouseDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new NotFoundException("Warehouse not found");

        var entity = await _unitOfWork.Warehouses.GetByIdAsync(id, cancellationToken);
        if (entity is null || !entity.IsActive) throw new NotFoundException("Warehouse not found");

        return MapToDto(entity);
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Warehouse
        {
            Name = dto.Name.Trim(),
            Location = dto.Location?.Trim(),
            IsActive = true
        };

        await _unitOfWork.Warehouses.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _cache.Remove("warehouses:all");
        _logger.LogInformation("Warehouse created. WarehouseId={WarehouseId}", entity.Id);
        return MapToDto(entity);
    }

    private static WarehouseDto MapToDto(Warehouse entity) =>
        new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Location = entity.Location,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAt
        };
}

