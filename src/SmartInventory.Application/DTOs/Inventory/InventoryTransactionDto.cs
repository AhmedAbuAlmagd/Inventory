namespace SmartInventory.Application.DTOs.Inventory;

public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string ProductSKU { get; set; } = default!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = default!;
    public string Type { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
    public string CreatedByUsername { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}

