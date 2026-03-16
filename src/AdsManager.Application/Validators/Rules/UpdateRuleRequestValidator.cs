using AdsManager.Application.DTOs.Rules;
using FluentValidation;

namespace AdsManager.Application.Validators.Rules;

public sealed class UpdateRuleRequestValidator : AbstractValidator<UpdateRuleRequest>
{
    public UpdateRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Threshold).GreaterThanOrEqualTo(0);
    }
}
