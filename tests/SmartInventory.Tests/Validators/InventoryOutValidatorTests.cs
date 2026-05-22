using FluentAssertions;
using SmartInventory.Application.DTOs.Inventory;
using SmartInventory.Application.Validators;

namespace SmartInventory.Tests.Validators;

public class InventoryOutValidatorTests
{
    private readonly InventoryOutValidator _validator = new();

    [Fact]
    public void Quantity_Zero_FailsValidation()
    {
        var dto = new InventoryOutDto
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = 0
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InventoryOutDto.Quantity));
    }

    [Fact]
    public void Quantity_Negative_FailsValidation()
    {
        var dto = new InventoryOutDto
        {
            ProductId = 1,
            WarehouseId = 1,
            Quantity = -1
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(InventoryOutDto.Quantity));
    }
}

