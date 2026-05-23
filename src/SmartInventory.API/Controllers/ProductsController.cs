using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.API.Contracts;
using SmartInventory.Application.DTOs.Common;
using SmartInventory.Application.DTOs.Products;
using SmartInventory.Application.Interfaces;

namespace SmartInventory.API.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Gets products with pagination and optional search.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size (server may clamp to a safe range).</param>
    /// <param name="search">Search term applied to name, SKU, and category.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<ProductDto>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _productService.GetAllAsync(page, pageSize, search, category, isActive, minPrice, maxPrice, cancellationToken));
    }

    /// <summary>
    /// Gets a product by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Gets all unique product categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetCategoriesAsync(cancellationToken));
    }

    /// <summary>
    /// Creates a new product (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var result = await _productService.CreateAsync(dto, cancellationToken);
        return StatusCode(201, result);
    }

    /// <summary>
    /// Updates a product (Admin only).
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        return Ok(await _productService.UpdateAsync(id, dto, cancellationToken));
    }

    /// <summary>
    /// Soft-deletes a product (Admin only).
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return Ok(null);
    }
}
