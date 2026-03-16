using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces.Meta;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/meta")]
public sealed class MetaAdsController : ControllerBase
{
    private readonly IMetaAdsService _metaAdsService;

    public MetaAdsController(IMetaAdsService metaAdsService)
    {
        _metaAdsService = metaAdsService;
    }

    [HttpGet("ad-accounts")]
    public async Task<IActionResult> GetAdAccounts([FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        var result = await _metaAdsService.GetAdAccountsAsync(accessToken, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ad-accounts/{adAccountId}/campaigns")]
    public async Task<IActionResult> GetCampaigns([FromRoute] string adAccountId, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        var result = await _metaAdsService.GetCampaignsAsync(adAccountId, accessToken, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ad-accounts/{adAccountId}/campaigns")]
    public async Task<IActionResult> CreateCampaign([FromRoute] string adAccountId, [FromQuery] string accessToken, [FromBody] MetaCampaignCreateRequest request, CancellationToken cancellationToken)
    {
        var id = await _metaAdsService.CreateCampaignAsync(adAccountId, request, accessToken, cancellationToken);
        return Ok(new { id });
    }

    [HttpPatch("campaigns/status")]
    public async Task<IActionResult> UpdateCampaignStatus([FromQuery] string accessToken, [FromBody] MetaCampaignStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        await _metaAdsService.UpdateCampaignStatusAsync(request, accessToken, cancellationToken);
        return NoContent();
    }

    [HttpPost("ad-accounts/{adAccountId}/adsets")]
    public async Task<IActionResult> CreateAdSet([FromRoute] string adAccountId, [FromQuery] string accessToken, [FromBody] MetaAdSetCreateRequest request, CancellationToken cancellationToken)
    {
        var id = await _metaAdsService.CreateAdSetAsync(adAccountId, request, accessToken, cancellationToken);
        return Ok(new { id });
    }

    [HttpPost("ads")]
    public async Task<IActionResult> CreateAd([FromQuery] string accessToken, [FromBody] MetaAdCreateRequest request, CancellationToken cancellationToken)
    {
        var id = await _metaAdsService.CreateAdAsync(request, accessToken, cancellationToken);
        return Ok(new { id });
    }

    [HttpGet("ad-accounts/{adAccountId}/insights")]
    public async Task<IActionResult> GetInsights([FromRoute] string adAccountId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, [FromQuery] string accessToken, CancellationToken cancellationToken)
    {
        if (since > until)
            throw new ValidationException("La fecha since no puede ser mayor que until.");

        var result = await _metaAdsService.GetInsightsAsync(adAccountId, since, until, accessToken, cancellationToken);
        return Ok(result);
    }
}
