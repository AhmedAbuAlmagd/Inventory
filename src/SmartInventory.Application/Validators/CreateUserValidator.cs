using FluentValidation;
using SmartInventory.Application.DTOs.Auth;

namespace SmartInventory.Application.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches(@"^[A-Za-z0-9_]+$").WithMessage("Username can only contain letters, numbers and underscores");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number");
        RuleFor(x => x.Role).IsInEnum();
    }
}

