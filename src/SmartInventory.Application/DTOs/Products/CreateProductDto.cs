namespace SmartInventory.Application.DTOs.Products;

public class CreateProductDto
{
    public string Name { get; set; } = default!;
    public string SKU { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
}

