using FluentAssertions;
using SmartInventory.Application.DTOs.Products;
using SmartInventory.Application.Validators;

namespace SmartInventory.Tests.Validators;

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void SKU_WithSpaces_FailsValidation()
    {
        var dto = new CreateProductDto
        {
            Name = "Test",
            SKU = "SKU 1",
            Price = 10
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductDto.SKU));
    }

    [Fact]
    public void Price_Zero_FailsValidation()
    {
        var dto = new CreateProductDto
        {
            Name = "Test",
            SKU = "SKU_1",
            Price = 0
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductDto.Price));
    }

    [Fact]
    public void Price_Positive_PassesValidation()
    {
        var dto = new CreateProductDto
        {
            Name = "Test",
            SKU = "SKU_1",
            Price = 10
        };

        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }
}

