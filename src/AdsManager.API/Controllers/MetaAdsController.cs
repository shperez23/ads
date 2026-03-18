using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdsManager.API.Authorization;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/meta")]
public sealed class MetaAdsController : ControllerBase
{
    private readonly IMetaAdsService _metaAdsService;
    private readonly ITenantProvider _tenantProvider;

    public MetaAdsController(IMetaAdsService metaAdsService, ITenantProvider tenantProvider)
    {
        _metaAdsService = metaAdsService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet("ad-accounts")]
    [Authorize(Policy = AuthorizationPolicies.AdAccountsManage)]
    public async Task<IActionResult> GetAdAccounts(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var result = await _metaAdsService.GetAdAccountsAsync(tenantId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ad-accounts/{adAccountId}/campaigns")]
    [Authorize(Policy = AuthorizationPolicies.CampaignsRead)]
    public async Task<IActionResult> GetCampaigns([FromRoute] string adAccountId, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var result = await _metaAdsService.GetCampaignsAsync(tenantId.Value, adAccountId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ad-accounts/{adAccountId}/campaigns")]
    [Authorize(Policy = AuthorizationPolicies.CampaignsWrite)]
    public async Task<IActionResult> CreateCampaign([FromRoute] string adAccountId, [FromBody] MetaCampaignCreateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var id = await _metaAdsService.CreateCampaignAsync(tenantId.Value, adAccountId, request, cancellationToken);
        return Ok(new { id });
    }

    [HttpPatch("campaigns/status")]
    [Authorize(Policy = AuthorizationPolicies.CampaignsWrite)]
    public async Task<IActionResult> UpdateCampaignStatus([FromBody] MetaCampaignStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        await _metaAdsService.UpdateCampaignStatusAsync(tenantId.Value, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("ad-accounts/{adAccountId}/adsets")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    public async Task<IActionResult> CreateAdSet([FromRoute] string adAccountId, [FromBody] MetaAdSetCreateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var id = await _metaAdsService.CreateAdSetAsync(tenantId.Value, adAccountId, request, cancellationToken);
        return Ok(new { id });
    }

    [HttpPost("ads")]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    public async Task<IActionResult> CreateAd([FromBody] MetaAdCreateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var id = await _metaAdsService.CreateAdAsync(tenantId.Value, request, cancellationToken);
        return Ok(new { id });
    }

    [HttpGet("ad-accounts/{adAccountId}/insights")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    public async Task<IActionResult> GetInsights([FromRoute] string adAccountId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, [FromQuery] string? level, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        if (since > until)
            throw new ValidationException("La fecha since no puede ser mayor que until.");

        var result = await _metaAdsService.GetInsightsAsync(tenantId.Value, adAccountId, since, until, level ?? "campaign", cancellationToken);
        return Ok(result);
    }
}
