using AdsManager.Application.DTOs.Rules;
using FluentValidation;

namespace AdsManager.Application.Validators.Rules;

public sealed class CreateRuleRequestValidator : AbstractValidator<CreateRuleRequest>
{
    public CreateRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Threshold).GreaterThanOrEqualTo(0);
    }
}
