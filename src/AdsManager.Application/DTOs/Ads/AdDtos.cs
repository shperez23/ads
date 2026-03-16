using AdsManager.Application.DTOs.Common;

namespace AdsManager.Application.DTOs.Ads;

public sealed record AdDto(
    Guid Id,
    string MetaAdId,
    Guid AdSetId,
    string Name,
    string Status,
    string CreativeJson,
    string? PreviewUrl);

public sealed record CreateAdRequest(
    Guid AdSetId,
    string Name,
    string Status,
    string CreativeJson,
    string? PreviewUrl);

public sealed record UpdateAdRequest(
    string Name,
    string Status,
    string CreativeJson,
    string? PreviewUrl);

public sealed record AdListRequest : PagedRequest
{
    public string? Status { get; init; }
    public Guid? CampaignId { get; init; }
}
