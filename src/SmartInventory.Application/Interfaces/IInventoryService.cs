using SmartInventory.Application.DTOs.Common;
using SmartInventory.Application.DTOs.Inventory;

namespace SmartInventory.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryTransactionDto> AddInAsync(InventoryInDto dto, int userId, CancellationToken cancellationToken = default);
    Task<InventoryTransactionDto> AddOutAsync(InventoryOutDto dto, int userId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<InventoryTransactionDto>> GetHistoryAsync(
        int page,
        int pageSize,
        int? productId,
        int? warehouseId,
        string? type,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
}
