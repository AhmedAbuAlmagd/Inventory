namespace SmartInventory.Application.DTOs.Warehouses;

public class CreateWarehouseDto
{
    public string Name { get; set; } = default!;
    public string? Location { get; set; }
}

