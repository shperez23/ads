using AdsManager.Application.DTOs.Auth;
using FluentValidation;

namespace AdsManager.Application.Validators.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.TenantName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.TenantSlug).NotEmpty().Matches("^[a-z0-9-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(64)
            .Matches("[A-Z]").WithMessage("La contraseña debe contener una mayúscula")
            .Matches("[a-z]").WithMessage("La contraseña debe contener una minúscula")
            .Matches("[0-9]").WithMessage("La contraseña debe contener un número");
    }
}
