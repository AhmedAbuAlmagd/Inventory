using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartInventory.Application.DTOs.Inventory;
using SmartInventory.Application.Exceptions;
using SmartInventory.Application.Interfaces;
using SmartInventory.Application.Services;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Tests.Services;

public class InventoryServiceTests
{
    [Fact]
    public async Task AddOut_InsufficientStock_ThrowsInsufficientStockException()
    {
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);
        productRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product { Id = 1, Name = "P1", SKU = "SKU1", Price = 1, IsActive = true });

        var warehouseRepo = new Mock<IWarehouseRepository>(MockBehavior.Strict);
        warehouseRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Warehouse { Id = 1, Name = "W1", IsActive = true });

        var txRepo = new Mock<IInventoryTransactionRepository>(MockBehavior.Strict);
        txRepo
            .Setup(r => r.GetCurrentStockAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.Products).Returns(productRepo.Object);
        uow.SetupGet(x => x.Warehouses).Returns(warehouseRepo.Object);
        uow.SetupGet(x => x.Transactions).Returns(txRepo.Object);

        var userLookup = new Mock<IUserLookupService>(MockBehavior.Strict);
        var service = new InventoryService(uow.Object, userLookup.Object, NullLogger<InventoryService>.Instance);

        var dto = new InventoryOutDto { ProductId = 1, WarehouseId = 1, Quantity = 6 };

        await FluentActions.Invoking(() => service.AddOutAsync(dto, userId: 1))
            .Should()
            .ThrowAsync<InsufficientStockException>();
    }
}

