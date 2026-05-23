using SmartInventory.Domain.Enums;
using SmartInventory.Domain.Entities;

namespace SmartInventory.Domain.Interfaces;

public interface IInventoryTransactionRepository : IRepository<InventoryTransaction>
{
    Task<InventoryTransaction?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<int> GetCurrentStockAsync(
        int productId,
        int warehouseId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<InventoryTransaction> Items, int TotalCount)> GetHistoryPagedAsync(
        int page,
        int pageSize,
        int? productId,
        int? warehouseId,
        TransactionType? type,
        string? search,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken = default);
}
