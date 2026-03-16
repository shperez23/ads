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
    string AccessToken,
    string? PreviewUrl);
