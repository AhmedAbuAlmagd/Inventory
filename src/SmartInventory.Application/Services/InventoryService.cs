using Microsoft.Extensions.Logging;
using SmartInventory.Application.DTOs.Common;
using SmartInventory.Application.DTOs.Inventory;
using SmartInventory.Application.Exceptions;
using SmartInventory.Application.Interfaces;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserLookupService _userLookupService;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(IUnitOfWork unitOfWork, IUserLookupService userLookupService, ILogger<InventoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _userLookupService = userLookupService;
        _logger = logger;
    }

    public async Task<InventoryTransactionDto> AddInAsync(
        InventoryInDto dto,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product is null || !product.IsActive) throw new NotFoundException("Product not found");

        var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId, cancellationToken);
        if (warehouse is null || !warehouse.IsActive) throw new NotFoundException("Warehouse not found");

        var transaction = new InventoryTransaction
        {
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            Type = TransactionType.In,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            Notes = dto.Notes?.Trim(),
            CreatedByUserId = userId
        };

        await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _unitOfWork.Transactions.GetByIdWithDetailsAsync(transaction.Id, cancellationToken);
        if (saved is null) throw new NotFoundException("Transaction not found");

        var usernames = await _userLookupService.GetUsernamesByIdsAsync([saved.CreatedByUserId], cancellationToken);
        var username = usernames.TryGetValue(saved.CreatedByUserId, out var u) ? u : "unknown";

        _logger.LogInformation(
            "Inventory IN. TransactionId={TransactionId} ProductId={ProductId} WarehouseId={WarehouseId} Qty={Qty} UserId={UserId}",
            saved.Id,
            saved.ProductId,
            saved.WarehouseId,
            saved.Quantity,
            saved.CreatedByUserId);

        return MapToDto(saved, username);
    }

    public async Task<InventoryTransactionDto> AddOutAsync(
        InventoryOutDto dto,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId, cancellationToken);
        if (product is null || !product.IsActive) throw new NotFoundException("Product not found");

        var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId, cancellationToken);
        if (warehouse is null || !warehouse.IsActive) throw new NotFoundException("Warehouse not found");

        var currentStock = await _unitOfWork.Transactions.GetCurrentStockAsync(dto.ProductId, dto.WarehouseId, cancellationToken);
        if (dto.Quantity > currentStock)
        {
            throw new InsufficientStockException($"Insufficient stock. Available: {currentStock}, Requested: {dto.Quantity}");
        }

        var transaction = new InventoryTransaction
        {
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            Type = TransactionType.Out,
            Quantity = dto.Quantity,
            UnitPrice = 0,
            Notes = dto.Notes?.Trim(),
            CreatedByUserId = userId
        };

        await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _unitOfWork.Transactions.GetByIdWithDetailsAsync(transaction.Id, cancellationToken);
        if (saved is null) throw new NotFoundException("Transaction not found");

        var usernames = await _userLookupService.GetUsernamesByIdsAsync([saved.CreatedByUserId], cancellationToken);
        var username = usernames.TryGetValue(saved.CreatedByUserId, out var u) ? u : "unknown";

        _logger.LogInformation(
            "Inventory OUT. TransactionId={TransactionId} ProductId={ProductId} WarehouseId={WarehouseId} Qty={Qty} UserId={UserId}",
            saved.Id,
            saved.ProductId,
            saved.WarehouseId,
            saved.Quantity,
            saved.CreatedByUserId);

        return MapToDto(saved, username);
    }

    public async Task<PagedResultDto<InventoryTransactionDto>> GetHistoryAsync(
        int page,
        int pageSize,
        int? productId,
        int? warehouseId,
        string? type,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = NormalizePageSize(pageSize, 20, 200);

        TransactionType? parsedType = null;
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<TransactionType>(type, true, out var txType))
        {
            parsedType = txType;
        }

        var (items, total) = await _unitOfWork.Transactions.GetHistoryPagedAsync(
            page,
            pageSize,
            productId,
            warehouseId,
            parsedType,
            search,
            fromUtc,
            toUtc,
            cancellationToken);

        var userIds = items.Select(x => x.CreatedByUserId).Distinct().ToArray();
        var usernameById = userIds.Length == 0
            ? new Dictionary<int, string>()
            : await _userLookupService.GetUsernamesByIdsAsync(userIds, cancellationToken);

        return new PagedResultDto<InventoryTransactionDto>
        {
            Items = items.Select(t =>
            {
                var username = usernameById.TryGetValue(t.CreatedByUserId, out var u) ? u : "unknown";
                return MapToDto(t, username);
            }).ToArray(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static InventoryTransactionDto MapToDto(InventoryTransaction t, string createdByUsername) =>
        new()
        {
            Id = t.Id,
            ProductId = t.ProductId,
            ProductName = t.Product.Name,
            ProductSKU = t.Product.SKU,
            WarehouseId = t.WarehouseId,
            WarehouseName = t.Warehouse.Name,
            Type = t.Type.ToString(),
            Quantity = t.Quantity,
            UnitPrice = t.UnitPrice,
            Notes = t.Notes,
            CreatedByUsername = createdByUsername,
            CreatedAtUtc = t.CreatedAt
        };

    private static int NormalizePageSize(int pageSize, int defaultSize, int maxSize)
    {
        if (pageSize <= 0) return defaultSize;
        if (pageSize > maxSize) return maxSize;
        return pageSize;
    }
}
