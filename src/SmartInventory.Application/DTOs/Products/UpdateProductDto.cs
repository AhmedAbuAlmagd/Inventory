namespace SmartInventory.Application.DTOs.Products;

public class UpdateProductDto
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
}

