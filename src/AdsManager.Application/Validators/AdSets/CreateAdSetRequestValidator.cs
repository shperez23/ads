using AdsManager.Application.DTOs.AdSets;
using FluentValidation;

namespace AdsManager.Application.Validators.AdSets;

public sealed class CreateAdSetRequestValidator : AbstractValidator<CreateAdSetRequest>
{
    public CreateAdSetRequestValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DailyBudget).GreaterThan(0);
        RuleFor(x => x.BillingEvent).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OptimizationGoal).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetingJson).NotEmpty();
        RuleFor(x => x.BidStrategy).NotEmpty().MaximumLength(100);
    }
}
