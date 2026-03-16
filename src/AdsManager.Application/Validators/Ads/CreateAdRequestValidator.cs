using AdsManager.Application.DTOs.Ads;
using FluentValidation;

namespace AdsManager.Application.Validators.Ads;

public sealed class CreateAdRequestValidator : AbstractValidator<CreateAdRequest>
{
    public CreateAdRequestValidator()
    {
        RuleFor(x => x.AdSetId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CreativeJson).NotEmpty();
        RuleFor(x => x.PreviewUrl).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.PreviewUrl));
    }
}
