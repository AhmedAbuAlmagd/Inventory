namespace SmartInventory.Application.DTOs.Inventory;

public class InventoryOutDto
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

