using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.API.Contracts;
using SmartInventory.Application.DTOs.Common;
using SmartInventory.Application.DTOs.Inventory;
using SmartInventory.Application.Exceptions;
using SmartInventory.Application.Interfaces;

namespace SmartInventory.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    private int CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : throw new UnauthorizedException("Invalid token");
        }
    }

    /// <summary>
    /// Adds stock to inventory (transaction type: In).
    /// </summary>
    [HttpPost("in")]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> In([FromBody] InventoryInDto dto, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AddInAsync(dto, CurrentUserId, cancellationToken);
        return StatusCode(201, result);
    }

    /// <summary>
    /// Removes stock from inventory (transaction type: Out).
    /// </summary>
    [HttpPost("out")]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDto>), 201)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Out([FromBody] InventoryOutDto dto, CancellationToken cancellationToken)
    {
        var result = await _inventoryService.AddOutAsync(dto, CurrentUserId, cancellationToken);
        return StatusCode(201, result);
    }

    /// <summary>
    /// Gets inventory transaction history with pagination and optional filters.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<InventoryTransactionDto>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> History(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? productId = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _inventoryService.GetHistoryAsync(
            page,
            pageSize,
            productId,
            warehouseId,
            type,
            search,
            fromUtc,
            toUtc,
            cancellationToken));
    }
}
