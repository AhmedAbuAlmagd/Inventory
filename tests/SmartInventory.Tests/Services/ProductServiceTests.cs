using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartInventory.Application.DTOs.Products;
using SmartInventory.Application.Services;
using SmartInventory.Domain.Entities;
using SmartInventory.Domain.Interfaces;

namespace SmartInventory.Tests.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task GetById_ExistingId_ReturnsFromCache_OnSecondCall()
    {
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);
        productRepo
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product
            {
                Id = 1,
                Name = "P1",
                SKU = "SKU1",
                Price = 10,
                IsActive = true
            });

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.Products).Returns(productRepo.Object);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new ProductService(uow.Object, cache, NullLogger<ProductService>.Instance);

        var first = await service.GetByIdAsync(1);
        var second = await service.GetByIdAsync(1);

        first.Should().BeEquivalentTo(second);
        productRepo.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateSKU_ThrowsConflictException()
    {
        var productRepo = new Mock<IProductRepository>(MockBehavior.Strict);
        productRepo
            .Setup(r => r.SKUExistsAsync("SKU1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var uow = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uow.SetupGet(x => x.Products).Returns(productRepo.Object);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new ProductService(uow.Object, cache, NullLogger<ProductService>.Instance);

        var dto = new CreateProductDto { Name = "P1", SKU = "SKU1", Price = 10 };

        await FluentActions.Invoking(() => service.CreateAsync(dto))
            .Should()
            .ThrowAsync<SmartInventory.Application.Exceptions.ConflictException>();
    }
}

