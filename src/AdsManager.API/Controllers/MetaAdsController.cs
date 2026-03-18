using AdsManager.API.Authorization;
using AdsManager.Application.DTOs.Meta;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Meta;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(IReadOnlyCollection<MetaAdAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<MetaAdAccountDto>>> GetAdAccounts(CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var result = await _metaAdsService.GetAdAccountsAsync(tenantId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ad-accounts/{adAccountId}/campaigns")]
    [Authorize(Policy = AuthorizationPolicies.CampaignsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<MetaCampaignDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<MetaCampaignDto>>> GetCampaigns([FromRoute] string adAccountId, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var result = await _metaAdsService.GetCampaignsAsync(tenantId.Value, adAccountId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("ad-accounts/{adAccountId}/campaigns")]
    [Authorize(Policy = AuthorizationPolicies.CampaignsWrite)]
    [ProducesResponseType(typeof(MetaResourceIdentifierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MetaResourceIdentifierDto>> CreateCampaign([FromRoute] string adAccountId, [FromBody] MetaCampaignCreateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var id = await _metaAdsService.CreateCampaignAsync(tenantId.Value, adAccountId, request, cancellationToken);
        return Ok(new MetaResourceIdentifierDto(id));
    }

    [HttpPatch("campaigns/status")]
    [Authorize(Policy = AuthorizationPolicies.CampaignsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UpdateCampaignStatus([FromBody] MetaCampaignStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        await _metaAdsService.UpdateCampaignStatusAsync(tenantId.Value, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("ad-accounts/{adAccountId}/adsets")]
    [Authorize(Policy = AuthorizationPolicies.AdSetsWrite)]
    [ProducesResponseType(typeof(MetaResourceIdentifierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MetaResourceIdentifierDto>> CreateAdSet([FromRoute] string adAccountId, [FromBody] MetaAdSetCreateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var id = await _metaAdsService.CreateAdSetAsync(tenantId.Value, adAccountId, request, cancellationToken);
        return Ok(new MetaResourceIdentifierDto(id));
    }

    [HttpPost("ads")]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    [ProducesResponseType(typeof(MetaResourceIdentifierDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MetaResourceIdentifierDto>> CreateAd([FromBody] MetaAdCreateRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var id = await _metaAdsService.CreateAdAsync(tenantId.Value, request, cancellationToken);
        return Ok(new MetaResourceIdentifierDto(id));
    }

    [HttpGet("ad-accounts/{adAccountId}/insights")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<MetaInsightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyCollection<MetaInsightDto>>> GetInsights([FromRoute] string adAccountId, [FromQuery] DateOnly since, [FromQuery] DateOnly until, [FromQuery] string? level, CancellationToken cancellationToken)
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
