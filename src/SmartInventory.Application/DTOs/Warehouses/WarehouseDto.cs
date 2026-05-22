namespace SmartInventory.Application.DTOs.Warehouses;

public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Location { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

