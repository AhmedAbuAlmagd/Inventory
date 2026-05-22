using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.API.Contracts;
using SmartInventory.Application.DTOs.Warehouses;
using SmartInventory.Application.Interfaces;

namespace SmartInventory.API.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    /// <summary>
    /// Gets all active warehouses.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WarehouseDto>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.GetAllAsync(cancellationToken));
    }

    /// <summary>
    /// Gets a warehouse by id.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Creates a new warehouse (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto, CancellationToken cancellationToken)
    {
        var result = await _warehouseService.CreateAsync(dto, cancellationToken);
        return StatusCode(201, result);
    }
}
