using FluentValidation;
using SmartInventory.Application.DTOs.Inventory;

namespace SmartInventory.Application.Validators;

public class InventoryInValidator : AbstractValidator<InventoryInDto>
{
    public InventoryInValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be at least 1");
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
    }
}

