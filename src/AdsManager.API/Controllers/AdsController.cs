using AdsManager.API.Authorization;
using AdsManager.Application.Common;
using AdsManager.Application.DTOs.Ads;
using AdsManager.Application.DTOs.Common;
using AdsManager.Application.DTOs.Insights;
using AdsManager.Application.Interfaces;
using AdsManager.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdsManager.API.Controllers;

[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ads")]
public sealed class AdsController : ControllerBase
{
    private readonly IAdsService _adsService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IReportService _reportService;

    public AdsController(IAdsService adsService, IReportService reportService, ITenantProvider tenantProvider)
    {
        _adsService = adsService;
        _reportService = reportService;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdsRead)]
    [ProducesResponseType(typeof(Result<PagedResponse<AdDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<PagedResponse<AdDto>>>> GetAds([FromQuery] AdListRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.GetAdsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdsRead)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdDto>>> GetAdById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.GetAdByIdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id:guid}/insights")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    [ProducesResponseType(typeof(Result<IReadOnlyCollection<InsightDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<IReadOnlyCollection<InsightDto>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<IReadOnlyCollection<InsightDto>>>> GetInsightsByAd([FromRoute] Guid id, [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _reportService.GetAdInsightsAsync(id, dateFrom, dateTo, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdDto>>> Create([FromBody] CreateAdRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.CreateAdAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdDto>>> UpdateAd([FromRoute] Guid id, [FromBody] UpdateAdRequest request, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.UpdateAdAsync(id, request, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/pause")]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdDto>>> PauseAd([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.PauseAdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPut("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.AdsWrite)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AdDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Result<AdDto>>> ActivateAd([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.GetTenantId().HasValue)
            return Unauthorized();

        var result = await _adsService.ActivateAdAsync(id, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
