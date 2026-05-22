namespace SmartInventory.Domain.Entities;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InventoryTransaction> Transactions { get; set; } = [];
}
