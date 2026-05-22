using FluentValidation;
using SmartInventory.Application.DTOs.Warehouses;

namespace SmartInventory.Application.Validators;

public class CreateWarehouseValidator : AbstractValidator<CreateWarehouseDto>
{
    public CreateWarehouseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).MaximumLength(500).When(x => x.Location is not null);
    }
}

