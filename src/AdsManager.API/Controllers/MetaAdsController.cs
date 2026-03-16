using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces.Meta;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    public async Task<IActionResult> GetAdAccounts(CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _metaAdsService.GetAdAccountsAsync(tenantId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ad-accounts/{adAccountId}/campaigns")]
    public async Task<IActionResult> GetCampaigns([FromRoute] string adAccountId, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var result = await _metaAdsService.GetCampaignsAsync(tenantId, adAccountId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ad-accounts/{adAccountId}/campaigns")]
    public async Task<IActionResult> CreateCampaign([FromRoute] string adAccountId, [FromBody] MetaCampaignCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var id = await _metaAdsService.CreateCampaignAsync(tenantId, adAccountId, request, cancellationToken);
        return Ok(new { id });
    }

    [HttpPatch("campaigns/status")]
    public async Task<IActionResult> UpdateCampaignStatus([FromBody] MetaCampaignStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        await _metaAdsService.UpdateCampaignStatusAsync(tenantId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("ad-accounts/{adAccountId}/adsets")]
    public async Task<IActionResult> CreateAdSet([FromRoute] string adAccountId, [FromBody] MetaAdSetCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var id = await _metaAdsService.CreateAdSetAsync(tenantId, adAccountId, request, cancellationToken);
        return Ok(new { id });
    }

    [HttpPost("ads")]
    public async Task<IActionResult> CreateAd([FromBody] MetaAdCreateRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        var id = await _metaAdsService.CreateAdAsync(tenantId, request, cancellationToken);
        return Ok(new { id });
    }

    [HttpGet("ad-accounts/{adAccountId}/insights")]
    public async Task<IActionResult> GetInsights([FromRoute] string adAccountId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
            return Unauthorized();

        if (since > until)
            throw new ValidationException("La fecha since no puede ser mayor que until.");

        var result = await _metaAdsService.GetInsightsAsync(tenantId, adAccountId, since, until, cancellationToken);
        return Ok(result);
    }

    private bool TryGetTenantId(out Guid tenantId)
    {
        var tenantClaim = User.FindFirstValue("tenantId");
        return Guid.TryParse(tenantClaim, out tenantId);
    }
}
